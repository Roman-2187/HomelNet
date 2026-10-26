using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.DbInfrastructure
{
    internal interface ISchemaSqlGenerator
    {
        string GenerateCreateTableSql(TableSchema schema);

        // 🎯 ОБА метода теперь железно возвращают объект SqlQueryCommand!
        SqlQueryCommand GenerateTableExistsSql(string tableName);
        SqlQueryCommand GenerateGetTableStructureSql(string tableName);
    }
}
