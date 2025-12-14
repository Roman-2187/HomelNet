using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Schemes;


namespace HomeNetCore.Data.Interfaces
{
    public interface ISchemaSqlInitializer
    {
        string GenerateCreateTableSql(TableSchema schema);
        string GenerateTableExistsSql(string? tableName);
        string GenerateGetTableStructureSql(string? tableName); 
        
    }

}
