using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System;
using System.Collections.Generic;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// Специализированный инициализатор схем для СУБД SQLite.
    /// Инкапсулирует специфику системных таблиц и PRAGMA-команд.
    /// </summary>
    public class SqliteSchemaInitializer : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public SqliteSchemaInitializer(ISchemaAdapter adapter, ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null)
            {
                _logger.LogError("[SQLITE ИНИЦИАЛИЗАТОР] Схема таблицы при генерации CREATE TABLE не может быть null");
                throw new ArgumentNullException(nameof(schema));
            }

            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует в схеме.");
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            // Для SQLite оборачиваем имя таблицы в безопасные кавычки по твоему канону
            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым при проверке существования", nameof(tableName));

            // В SQLite проверяем наличие таблицы через служебную таблицу sqlite_master
            return $@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}';";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым для получения структуры", nameof(tableName));

            // Никаких плейсхолдеров и замен подстрок на лету! Жесткий, застрахованный SQLite-синтаксис
            return $@"PRAGMA table_info(""{tableName}"");";
        }
    }
}
