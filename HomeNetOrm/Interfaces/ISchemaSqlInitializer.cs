using HomeNetOrm.Data.Schemes.CreateSchemaBd;


namespace HomeNetOrm.Interfaces
{
    public interface ISchemaSqlInitializer
    {
        string GenerateCreateTableSql(TableSchema schema);
        string GenerateTableExistsSql(string? tableName);
        string GenerateGetTableStructureSql(string? tableName); 
        
    }

}
