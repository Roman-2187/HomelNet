using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Interfaces
{
    /// <summary>
    /// Внутренний паспорт СУБД. 
    /// Отвечает исключительно за взаимный перевод типов данных C# и конкретного диалекта базы.
    /// </summary>
    public interface IDbProviderSpecification
    {
        /// <summary>
        /// Переводит внутренний тип ORM в строковое представление типа конкретной СУБД для сборки CREATE TABLE.
        /// </summary>
        string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length);

        /// <summary>
        /// Переводит сырое текстовое название типа из системных каталогов СУБД обратно в енам ColumnType.
        /// </summary>
        ColumnType ParsePropertyType(string dbType);
    }
}
