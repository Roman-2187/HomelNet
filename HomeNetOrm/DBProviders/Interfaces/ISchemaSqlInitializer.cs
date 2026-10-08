using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Interfaces
{
    /// <summary>
    /// Контракт генератора системных SQL-скриптов инициализации, проверки и сверки схем БД.
    /// </summary>
    public interface ISchemaSqlInitializer
    {
        string GenerateCreateTableSql(TableSchema schema);
        string GenerateTableExistsSql(string tableName);
        string GenerateGetTableStructureSql(string tableName);
    }
}
