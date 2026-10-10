using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres
{
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
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует.");
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);
            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));
            return $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @CleanName;";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // ⚡ ИСПРАВЛЕНО: c.is_nullable = 'YES' выдаст строгий boolean, который Dapper без проблем зальет в bool!
            return $@"
        SELECT 
            c.column_name AS Name, 
            c.data_type AS DataType, 
            (c.is_nullable = 'YES') AS IsNullable,
            CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'YES' ELSE 'NO' END AS KeyType,
            c.column_default AS ExtraInfo
        FROM information_schema.columns c
        LEFT JOIN information_schema.key_column_usage kcu 
            ON c.table_schema = kcu.table_schema AND c.table_name = kcu.table_name AND c.column_name = kcu.column_name
        LEFT JOIN information_schema.table_constraints tc 
            ON kcu.table_schema = tc.table_schema AND kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name AND tc.constraint_type = 'PRIMARY KEY'
        WHERE c.table_schema = 'public' AND c.table_name = @TableName;";
        }

    }
}
