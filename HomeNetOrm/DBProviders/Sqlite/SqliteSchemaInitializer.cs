using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeNetOrm.DBProviders.Sqlite
{
    internal class SqliteSchemaInitializer : ISchemaSqlInitializer
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
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует.");

            // 1. Получаем дефиниции колонок
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            // 2. Находим все колонки первичного ключа
            var pkColumns = schema.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();
            sb.Append($@"CREATE TABLE IF NOT EXISTS ""{tableName}"" (");
            sb.Append(string.Join(", ", columnDefinitions));

            // 🎯 ЖЕЛЕЗОБЕТОННЫЙ ФИКС ДЛЯ СОСТАВНОГО КЛЮЧА В SQLITE:
            // Если ключей больше одного (как в таблице DeviceSettings) — дописываем ограничение в самый конец
            if (pkColumns.Count > 1)
            {
                string pkFieldNames = string.Join(", ", pkColumns.Select(c => $"\"{c.Name}\""));
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.Append(");");
            return sb.ToString();
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));
            return $@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@cleanName;";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));
            return $@"
        SELECT 
            name AS Name, 
            type AS DataType, 
            CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
            CASE WHEN pk >= 1 THEN 'YES' ELSE 'NO' END AS KeyType, -- 🎯 ФИКС: Любое число >= 1 — это первичный ключ!
            dflt_value AS ExtraInfo
            FROM pragma_table_info(@TableName);";
        }

    }
}
