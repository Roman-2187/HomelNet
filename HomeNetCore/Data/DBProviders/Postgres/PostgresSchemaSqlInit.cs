using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;
using WpfHomeNet.Data.DBProviders.Postgres;

namespace HomeNetCore.Data.DBProviders.Postgres
{
    public class PostgresSchemaSqlInit: ISchemaSqlInitializer  
    {


        private readonly ISchemaAdapter _adapter;
        private ILogger _logger;


        public PostgresSchemaSqlInit(ILogger logger,ISchemaAdapter schemaAdapter)
        {
            _adapter = schemaAdapter;
            _logger = logger;
        }
       


        public  string GenerateCreateTableSql(TableSchema schema)
        {
            var columnsSql = string.Join(", ", schema.Columns.Select(c =>
                $"{c.Name} {c.Type}"));
            return $"CREATE TABLE {schema.TableName} ({columnsSql})";
        }



        public string GenerateTableExistsSql(string TableName)
        {
            return $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '{TableName}')";
        }

        public string GenerateGetTableStructureSql(string tableName)
        {
            return @"
            SELECT column_name,
                data_type,
                character_maximum_length,
                is_nullable,
                column_key,
                extra
            FROM information_schema.columns
            WHERE table_name = @tableName";
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
