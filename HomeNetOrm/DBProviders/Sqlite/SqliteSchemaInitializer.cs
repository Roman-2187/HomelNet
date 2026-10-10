using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite
{
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
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует.");
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);
            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // ⚡ ИСПРАВЛЕНО: @cleanName с маленькой буквы!
            return $@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@cleanName;";
        }


        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // ⚡ ИСПРАВЛЕНО: [notnull] в квадратных скобках!
            return $@"
        SELECT 
            name AS Name, 
            type AS DataType, 
            CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
            CASE WHEN pk = 1 THEN 'YES' ELSE 'NO' END AS KeyType,
            dflt_value AS ExtraInfo
        FROM pragma_table_info(@TableName);";
        }

    }
}
