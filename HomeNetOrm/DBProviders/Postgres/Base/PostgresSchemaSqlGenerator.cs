using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.DBProviders.Base;
using System.Text;
using Dapper;

namespace HomeNetOrm.DBProviders.Postgres.Base
{
    internal class PostgresSchemaSqlGenerator : ISchemaSqlGenerator
    {
        private readonly IDbProviderSpecification _spec;
        private readonly ILogger _logger;

        public PostgresSchemaSqlGenerator(IDbProviderSpecification spec, ILogger logger)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
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

            sb.AppendLine(");");

            if (schema.UseVersionTrigger)
            {
                string sequenceName = $"seq_{tableName}_version";
                string triggerName = $"trg_{tableName}_update_version";

                sb.AppendLine($@"
                CREATE SEQUENCE IF NOT EXISTS ""{sequenceName}"" START WITH 1;
                CREATE OR REPLACE FUNCTION fn_update_version_column()
                RETURNS TRIGGER AS $$
                BEGIN
                NEW.""version"" := nextval('seq_' || TG_TABLE_NAME || '_version');
                RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                DROP TRIGGER IF EXISTS ""{triggerName}"" ON ""{tableName}"";
                CREATE TRIGGER ""{triggerName}""
                BEFORE INSERT OR UPDATE ON ""{tableName}""
                FOR EACH ROW
                EXECUTE FUNCTION fn_update_version_column();
                ");
            }

            return sb.ToString();
        }

        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            var p = new DynamicParameters();
            p.Add("CleanName", tableName.Trim('"', '\'').ToLower());

            return new SqlQueryCommand(
                $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @CleanName;",
                p
            );
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));
            return new SqlQueryCommand($@"
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
                WHERE c.table_schema = 'public' AND c.table_name = '{tableName}';"
            );
        }
    }
}
