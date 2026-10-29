using System.Text;
using Dapper;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres
{
    internal class PostgresSchemaSqlGenerator : ISchemaSqlGenerator
    {
        private readonly IDbProviderSpecification _spec;

        // 🔥 Конструктор принимает СТРОГО ОДИН параметр — спецификацию типов!
        public PostgresSchemaSqlGenerator(IDbProviderSpecification spec)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (string.IsNullOrEmpty(schema.TableName) || schema.Columns == null)
                return string.Empty;

            var pkColumns = schema.Columns.Where(c => c != null && c.IsPrimaryKey).ToList();
            var sb = new StringBuilder();

            // 🧙‍♂️ Чистый, читаемый SQL без лишней шелухи транслятора
            // Мы явно пишем двойные кавычки для Postgres, так как этот класс создан ТОЛЬКО под него
            sb.Append($"CREATE TABLE IF NOT EXISTS \"{GenericSchemaAdapter.ToSnakeCase(schema.TableName)}\" (");

            // Колонки штампуются через существующий адаптер спецификации типов данных
            var definitions = GenericSchemaAdapter.GetColumnDefinitions(_spec, schema);
            sb.Append(string.Join(", ", definitions));

            // Если ключ составной — собираем блок PRIMARY KEY прямо на месте
            if (pkColumns.Count > 1)
            {
                var pkFieldNames = string.Join(", ", pkColumns.Select(c => $"\"{c.Name ?? "id"}\""));
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.AppendLine(");");
            return sb.ToString();
        }

        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            string cleanName = tableName.Trim('"', '\'').ToLower();

            // Идеально плоский текст, который Dapper выполнит со скоростью света
            var sql = $"SELECT COUNT(*) FROM {MetaTablesRegistry} WHERE table_schema = '{DefaultSchemaName}' AND table_name = @CleanName;";

            var p = new DynamicParameters();
            p.Add("CleanName", cleanName);

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // Системный запрос к метаданным Postgres — кристальная чистота, 
            // а все служебные таблицы уехали в подвал!
            var sql = $@"
                SELECT 
                c.column_name AS Name, 
                c.data_type AS DataType, 
                (c.is_nullable = 'YES') AS IsNullable,
                CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'YES' ELSE 'NO' END AS KeyType,
                c.column_default AS ExtraInfo
                FROM {MetaColumnsRegistry} c
                LEFT JOIN {MetaConstraintsUsageRegistry} kcu 
                ON c.table_schema = kcu.table_schema AND c.table_name = kcu.table_name AND c.column_name = kcu.column_name
                LEFT JOIN {MetaConstraintsRegistry} tc 
                ON kcu.table_schema = tc.table_schema AND kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name AND tc.constraint_type = 'PRIMARY KEY'
                WHERE c.table_schema = '{DefaultSchemaName}' AND c.table_name = '{tableName}';";

            return new SqlQueryCommand(sql);
        }

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Системные константы Postgres, спрятанные от обзора
        // =========================================================================
        private const string DefaultSchemaName = "public";
        private const string MetaTablesRegistry = "information_schema.tables";
        private const string MetaColumnsRegistry = "information_schema.columns";
        private const string MetaConstraintsRegistry = "information_schema.table_constraints";
        private const string MetaConstraintsUsageRegistry = "information_schema.key_column_usage";
    }
}
