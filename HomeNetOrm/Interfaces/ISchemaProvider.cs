using HomeNetOrm.DBProviders.Extensions;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces
{
    
        public interface ISchemaProvider
        {
            /// <summary>
            /// Извлекает актуальную схему таблицы напрямую из системных каталогов СУБД.
            /// </summary>
            /// <param name="tableName">Имя проверяемой таблицы в базе данных.</param>
            /// <returns>Готовая объектная модель схемы TableSchema для последующей сверки.</returns>
            Task<TableSchema> GetActualTableSchemaAsync(string? tableName);
        }
    
}
