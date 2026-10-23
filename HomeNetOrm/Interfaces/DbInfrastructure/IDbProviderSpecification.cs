using HomeNetOrm.Enums;

namespace HomeNetOrm.Interfaces.DbInfrastructure
{
    /// <summary>
    /// Внутренний паспорт СУБД. 
    /// Отвечает исключительно за взаимный перевод типов данных C# и конкретного диалекта базы.
    /// </summary>
    internal interface IDbProviderSpecification
    {
        // 🔥 ДОБАВЛЯЕМ ФОРМАТ: Спецификация обязана говорить, какой регистр использует база!
        NameFormat TableFormat { get; }

        /// <summary>
        /// Переводит внутренний тип ORM в строковое представление типа конкретной СУБД для сборки CREATE TABLE.
        /// </summary>
        string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length);

        /// <summary>
        /// Переводит сырое текстовое название типа из системных каталогов СУБД обратно в енам ColumnType.
        /// </summary>
        ColumnType ParsePropertyType(string dbType);



        // 🔥 ДОБАВЛЯЕМ В ИНТЕРФЕЙС: Контракт на сборку фич колонки
        string? GetColumnFeatures(ColumnType type, bool isPk, bool isAi, object? defaultValue, bool isCreatedAt);
    }
}
