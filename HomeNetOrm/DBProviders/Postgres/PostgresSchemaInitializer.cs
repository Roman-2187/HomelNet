using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using System.Text;

namespace HomeNetOrm.DBProviders.Postgres
{
    internal class PostgresSchemaInitializer : ISchemaSqlInitializer
    {
        private readonly IDbProviderSpecification _spec;
        private readonly ILogger _logger;

        // 🔥 Вместо адаптера принимаем чистую спецификацию СУБД
        public PostgresSchemaInitializer(IDbProviderSpecification spec, ILogger logger)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            // 🎯 ФИКС: Переводим имя таблицы через статический метод расширения строки!
            string tableName = schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            var pkColumns = schema.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();
            sb.Append($@"CREATE TABLE IF NOT EXISTS ""{tableName}"" (");

            // 🔥 ФИКС: Прокидываем спецификацию в статический билдер колонок без адаптеров
            sb.Append(SqlQueryBuilder.BuildCreateTableColumns(_spec, schema));

            // 🔥 ФИКС: Вызов статика для составного ключа в snake_case без адаптера
            if (pkColumns.Count > 1)
            {
                string pkFieldNames = SqlQueryBuilder.BuildCompositePrimaryKeyFields(schema, NameFormat.SnakeCase);
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
                NEW.""Version"" := nextval('seq_' || TG_TABLE_NAME || '_version');
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

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));
            return $@"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @CleanName;";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

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
