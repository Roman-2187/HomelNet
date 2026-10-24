using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.DBProviders.Base;
using System.Text;
using Dapper;

namespace HomeNetOrm.DBProviders.Sqlite.Base
{
    internal class SqliteSchemaInitializer : ISchemaSqlInitializer
    {
        private readonly IDbProviderSpecification _spec;
        private readonly ILogger _logger;

        public SqliteSchemaInitializer(IDbProviderSpecification spec, ILogger logger)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(schema.TableName) || schema.Columns == null)
                return string.Empty;

            string tableName = schema.TableName;
            var pkColumns = schema.Columns.Where(c => c != null && c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();
            sb.Append($@"CREATE TABLE IF NOT EXISTS ""{tableName}"" (");

            sb.Append(SqlQueryBuilder.BuildCreateTableColumns(_spec, schema));

            if (pkColumns.Count > 1)
            {
                string pkFieldNames = SqlQueryBuilder.BuildCompositePrimaryKeyFields(schema);
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.Append(");");
            return sb.ToString();
        }

        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // Заворачиваем параметр чисто на автопилоте для Dapper
            var p = new DynamicParameters();
            p.Add("CleanName", tableName.Trim('"', '\'').ToLower());

            return new SqlQueryCommand(
                $@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = @CleanName;",
                p
            );
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            return new SqlQueryCommand($@"
            SELECT 
            name AS Name, 
            type AS DataType, 
            CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
            CASE WHEN pk >= 1 THEN 'YES' ELSE 'NO' END AS KeyType,
            dflt_value AS ExtraInfo
            FROM pragma_table_info('{tableName}');"
            );
        }
    }
}
