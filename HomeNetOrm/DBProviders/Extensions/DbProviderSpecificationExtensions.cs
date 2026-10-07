using HomeNetOrm.Enums;
using System;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// Спецификация провайдера БД. Полный цифровой слепок настроек метаданных для конкретной СУБД.
    /// </summary>
    public class DbProviderSpecificationExtensions
    {
        // SQL-запросы для системных проверок структуры
        public string TableExistsQuery { get; }
        public string GetTableStructureQuery { get; }

        // Маппинг и парсинг типов
        public Func<ColumnType, bool, bool, int?, string> MapToSqlType { get; }
        public Func<string, ColumnType> ParsePropertyType { get; }

        // Жесткие индексы колонок для чтения системных таблиц СУБД
        public int NameIndex { get; }
        public int TypeIndex { get; }
        public int NullableIndex { get; }
        public int PrimaryKeyIndex { get; }
        public int ExtraInfoIndex { get; }

        public DbProviderSpecificationExtensions(
            string tableExistsQuery,
            string getTableStructureQuery,
            Func<ColumnType, bool, bool, int?, string> mapToSqlType,
            Func<string, ColumnType> parsePropertyType,
            int nameIndex,
            int typeIndex,
            int nullableIndex,
            int primaryKeyIndex,
            int extraInfoIndex)
        {
            TableExistsQuery = tableExistsQuery ?? throw new ArgumentNullException(nameof(tableExistsQuery));
            GetTableStructureQuery = getTableStructureQuery ?? throw new ArgumentNullException(nameof(getTableStructureQuery));
            MapToSqlType = mapToSqlType ?? throw new ArgumentNullException(nameof(mapToSqlType));
            ParsePropertyType = parsePropertyType ?? throw new ArgumentNullException(nameof(parsePropertyType));
            NameIndex = nameIndex;
            TypeIndex = typeIndex;
            NullableIndex = nullableIndex;
            PrimaryKeyIndex = primaryKeyIndex;
            ExtraInfoIndex = extraInfoIndex;
        }
    }
}
