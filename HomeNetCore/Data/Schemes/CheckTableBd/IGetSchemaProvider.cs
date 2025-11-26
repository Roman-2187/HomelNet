using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Data.Schemes.GetSchemaTableBd
{
    public interface IGetSchemaProvider
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
