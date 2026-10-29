using System.Text;
using Dapper;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite
{
    internal class SqliteSchemaGenerator : ISchemaSqlGenerator
    {
        private readonly IDbProviderSpecification _spec;

        public SqliteSchemaGenerator(IDbProviderSpecification spec)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(schema.TableName) || schema.Columns == null)
                return string.Empty;

            var pkColumns = schema.Columns.Where(c => c != null && c.IsPrimaryKey).ToList();
            var sb = new StringBuilder();

            // 1. Начало сборки таблицы
            sb.Append($"CREATE TABLE IF NOT EXISTS {schema.TableName} (");

            // 2. Генерируем дефиниции колонок напрямую через адаптер спецификации
            var definitions = GenericSchemaAdapter.GetColumnDefinitions(_spec, schema);
            sb.Append(string.Join(", ", definitions));

            // 3. Сборка составного первичного ключа
            if (pkColumns.Count > 1)
            {
                var pkFieldNames = string.Join(", ", pkColumns.Select(c => $"\"{c.Name}\""));
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.Append(");");
            return sb.ToString();
        }


        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // Читается мгновенно! Никакого мусора от говоруна
            var sql = $"SELECT COUNT(*) FROM {SqliteMasterTable} WHERE type='table' AND name = @CleanName;";

            var p = new DynamicParameters();
            p.Add("CleanName", tableName.Trim('"', '\'').ToLower());

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            var sql = $@"SELECT 
                name AS Name, 
                type AS DataType, 
                CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN pk >= 1 THEN 'YES' ELSE 'NO' END AS KeyType,
                dflt_value AS ExtraInfo
                FROM {PragmaTableInfoFunc}('{tableName}');";

            return new SqlQueryCommand(sql);
        }

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Специфичный системный синтаксис SQLite
        // =========================================================================
        private const string SqliteMasterTable = "sqlite_master";
        private const string PragmaTableInfoFunc = "pragma_table_info";
    }
}
