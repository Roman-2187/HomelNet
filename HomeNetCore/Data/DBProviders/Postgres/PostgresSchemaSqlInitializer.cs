using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeNetCore.Data.DBProviders.Postgres
{
    public class PostgresSchemaSqlInitializer : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public PostgresSchemaSqlInitializer(ILogger logger, ISchemaAdapter schemaAdapter)
        {
            _adapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null)
            {
                _logger.LogError("Схема таблицы при генерации CREATE TABLE не может быть null");
                throw new ArgumentNullException(nameof(schema));
            }

            // На вход подается уже адаптированная змейка-схема из DBInitializer
            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует в схеме.");

            // 🔥 ЖЕСТКОЕ ИСПРАВЛЕНИЕ: Вызываем прокачанный Postgres-адаптер для сборки всех SERIAL, VARCHAR и ограничений!
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogError("Имя таблицы для проверки существования не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null", nameof(tableName));
            }

            // Переводим в змейку под стандарты Postgres
            string escapedName = _adapter.ConvertTableName(tableName, NameFormat.SnakeCase);

            // Возвращаем честный подсчет строк (0 или 1), чтобы метод возвращал такой же ответ, как в SQLite
            return $@"SELECT COUNT(*) FROM information_schema.tables 
                      WHERE table_schema = 'public' AND table_name = '{escapedName}';";
        }

        public string GenerateGetTableStructureSql(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogError("Имя таблицы для чтения структуры не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null", nameof(tableName));
            }

            // ⚡ ФУНДАМЕНТАЛЬНОЕ ИСПРАВЛЕНИЕ: Родной, сложный SQL-запрос для PostgreSQL, 
            // который честно вытаскивает типы, длины, PRIMARY KEY и привязку к SERIAL (nextval)
            return @"
                SELECT 
                    c.column_name,
                    c.data_type,
                    c.character_maximum_length,
                    c.is_nullable,
                    CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'primary' ELSE '' END as key_type,
                    COALESCE(c.column_default, '') as extra_info
                FROM information_schema.columns c
                LEFT JOIN information_schema.key_column_usage kcu 
                    ON c.table_name = kcu.table_name 
                    AND c.column_name = kcu.column_name
                LEFT JOIN information_schema.table_constraints tc 
                    ON kcu.table_name = tc.table_name 
                    AND kcu.constraint_name = tc.constraint_name
                WHERE lower(c.table_name) = lower(@tableName)
                AND c.table_schema = 'public';";
        }
    }
}
