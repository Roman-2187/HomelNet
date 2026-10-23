using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using System.Text;

namespace HomeNetOrm.DBProviders.Sqlite
{
    internal class SqliteSchemaInitializer(IDbProviderSpecification spec, ILogger logger) : ISchemaSqlInitializer
    {
        private readonly IDbProviderSpecification _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public string GenerateCreateTableSql(TableSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            // 🎯 СИНХРОН С ПОСТГРЕ: Переводим имя таблицы строго в SnakeCase!
            string tableName = schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            var pkColumns = schema.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();
            sb.Append($@"CREATE TABLE IF NOT EXISTS ""{tableName}"" (");

            // 🔥 Прокидываем спецификацию в статический билдер колонок
            sb.Append(SqlQueryBuilder.BuildCreateTableColumns(_spec, schema));

            // 🔥 Вызов статика для составного ключа строго в SnakeCase
            if (pkColumns.Count > 1)
            {
                string pkFieldNames = SqlQueryBuilder.BuildCompositePrimaryKeyFields(schema, NameFormat.SnakeCase);
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.Append(");");
            return sb.ToString();
        }

        public string GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // 🎯 ФИКС: Сравниваем имя в системной таблице через LOWER, 
            // чтобы регистр входящего @cleanName больше никогда ничего не ломал!
            return $@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND LOWER(name) = LOWER(@cleanName);";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // 🎯 ФИКС: Переводим входящее имя строки в SnakeCase прямо в C#, 
            // так как pragma_table_info не умеет работать с динамическим LOWER в аргументе
            string snakeTableName = tableName.ConvertTableName(NameFormat.SnakeCase);

            return $@"
    SELECT 
        name AS Name, 
        type AS DataType, 
        CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
        CASE WHEN pk >= 1 THEN 'YES' ELSE 'NO' END AS KeyType,
        dflt_value AS ExtraInfo
    FROM pragma_table_info('{snakeTableName}');";
        }

    }
}
