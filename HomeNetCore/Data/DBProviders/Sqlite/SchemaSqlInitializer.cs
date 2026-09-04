using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using System;
using System.Collections.Generic;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    public class SchemaSqlInitializer : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public SchemaSqlInitializer(ILogger logger, ISchemaAdapter schemaAdapter)
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

            // На вход подается уже адаптированная змейка-схема из DBInitializer,
            // поэтому берем имя напрямую, оно уже в правильном регистре!
            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует в схеме.");

            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            return $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", columnDefinitions)})";
        }

        public string GenerateTableExistsSql(string? tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                _logger.LogError("Имя таблицы для проверки существования не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null");
            }

            // ⚡ ИСПРАВЛЕНИЕ: Переводим в змейку и подставляем именно ПРАВИЛЬНОЕ имя в запрос!
            string escapedName = _adapter.ConvertTableName(tableName, NameFormat.SnakeCase);

            return $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{escapedName}'";
        }

        public string GenerateGetTableStructureSql(string? tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                _logger.LogError("Имя таблицы для чтения структуры не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null");
            }

            string escapedName = _adapter.ConvertTableName(tableName, NameFormat.SnakeCase);

            return $"PRAGMA table_info(\"{escapedName}\")";
        }
    }
}
