using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;

namespace HomeNetCore.Data.Interfaces
{
    public interface ISchemaProvider
    {       
        /// <summary>
        /// получаем актуальную схему бд
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<TableSchema> GetActualTableSchemaAsync(string? tableName);

        ColumnType MapType(string? dbType);
    }




}
