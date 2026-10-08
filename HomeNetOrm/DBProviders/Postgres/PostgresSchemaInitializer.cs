using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres
{
    /// <summary>
    /// Специализированный инициализатор схем для СУБД PostgreSQL.
    /// Инкапсулирует работу с системным каталогом information_schema и разметкой public.
    /// </summary>
    public class PostgresSchemaInitializer : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public PostgresSchemaInitializer(ISchemaAdapter adapter, ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null)
            {
                _logger.LogError("[POSTGRES ИНИЦИАЛИЗАТОР] Схема таблицы при генерации CREATE TABLE не может быть null");
                throw new ArgumentNullException(nameof(schema));
            }

            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует в схеме.");
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            // В Postgres строго экранируем имя таблицы кавычками из-за чувствительности к регистру snake_case
            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым при проверке существования", nameof(tableName));

            // Смотрим в стандартную схему таблиц информационного каталога Postgres
            return $@"SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                          AND table_name = '{tableName}'
                      );";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым", nameof(tableName));

            // 🎯 ИСПРАВЛЕНО: Выдаем '1' и '0' в нижнем регистре ('yes'/'no'), 
            // чтобы GenericSchemaProvider гарантированно распознал Primary Key на любом диалекте!
            return $@"
        SELECT 
            c.column_name, 
            c.data_type, 
            '' as column_default,
            c.is_nullable,
            CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'yes' ELSE 'no' END::text as is_primary_key,
            '' as extra_info
        FROM information_schema.columns c
        LEFT JOIN information_schema.key_column_usage kcu 
            ON c.table_schema = kcu.table_schema 
            AND c.table_name = kcu.table_name 
            AND c.column_name = kcu.column_name
        LEFT JOIN information_schema.table_constraints tc 
            ON kcu.table_schema = tc.table_schema 
            AND kcu.table_name = tc.table_name 
            AND kcu.constraint_name = tc.constraint_name
            AND tc.constraint_type = 'PRIMARY KEY'
        WHERE c.table_schema = 'public' 
          AND c.table_name = '{tableName}';";
        }




    }
}
