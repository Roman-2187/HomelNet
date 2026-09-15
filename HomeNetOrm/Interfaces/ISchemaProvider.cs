using HomeNetCore.Enums;
using HomeNetOrm.Data.Schemes.CreateSchemaBd;

namespace HomeNetOrm.Interfaces
{
    public interface ISchemaProvider
    {       
        /// <summary>
        /// получаем актуальную схему бд
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<TableSchema> GetActualTableSchemaAsync(string? tableName);

     
    }




}
