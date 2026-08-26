using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using WpfHomeNet.Data.DBProviders.Postgres;

namespace HomeNetCore.Data.DBProviders.Postgres
{
    public class PostgresSchemaSqlInit : ISchemaSqlInitializer
    {


        private readonly ISchemaAdapter _adapter;
        private ILogger _logger;


        public PostgresSchemaSqlInit(ILogger logger, ISchemaAdapter schemaAdapter)
        {
            _adapter = schemaAdapter;
            _logger = logger;
        }



        public string GenerateCreateTableSql(TableSchema schema)
        {
            var columnsSql = string.Join(", ", schema.Columns.Select(c =>
                $"{c.Name} {c.Type}"));
            return $"CREATE TABLE {schema.TableName} ({columnsSql})";
        }



        public string GenerateTableExistsSql(string? tableName)
        {
            // Компилятор видит, что мы обработали null на самом входе
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не может быть пустым при проверке её существования.", nameof(tableName));
            }

            // После этой проверки tableName гарантированно не null, компилятор молчит
            return $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE lower(table_name) = '{tableName.ToLowerInvariant()}')";
        }

        public string GenerateGetTableStructureSql(string? tableName)
        {
            // Точно так же возвращаем string? для соответствия интерфейсу
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Имя таблицы не может быть пустым для получения её структуры.", nameof(tableName));
            }

            return @"
    SELECT column_name,
        data_type,
        character_maximum_length,
        is_nullable,
        column_key,
        extra
    FROM information_schema.columns
    WHERE lower(table_name) = lower(@tableName);";
        }


        public ColumnType MapDatabaseType(string dbType)
        {
            switch (dbType.ToLower())
            {
                case "integer": return ColumnType.Integer;
                case "character varying": return ColumnType.Varchar;
                default: return ColumnType.Unknown;
            }
        }
    }

}
