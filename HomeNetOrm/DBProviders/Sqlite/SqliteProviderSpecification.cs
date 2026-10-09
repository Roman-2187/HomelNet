using System;
using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Extensions; // Твой класс спецификации

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// Паспорт спецификации типов данных и индексов маппинга для SQLite.
    /// </summary>
    public class SqliteProviderSpecification : DbProviderSpecificationExtensions
    {
        public SqliteProviderSpecification() : base(
            // Передаем пустые строки, так как за SQL теперь отвечают классы инициализации!
            tableExistsQuery: string.Empty,
            getTableStructureQuery: string.Empty,

            mapToSqlType: (type, isPk, isAi, length) => type switch
            {
                ColumnType.Integer => "INTEGER",
                ColumnType.Varchar => "TEXT",
                ColumnType.Boolean => "BOOLEAN",
                ColumnType.Guid => "TEXT",

                // 🎯 Единый тип времени для ОРМ
                ColumnType.DateTimeOffset => "DATETIME",
                _ => "TEXT"
            },
            parsePropertyType: dbType => dbType.ToLower() switch
            {
                "integer" => ColumnType.Integer,
                "text" or "varchar" => ColumnType.Varchar,
                "boolean" => ColumnType.Boolean,

                // ⚡ Любые форматы времени из SQLite переводим в глобальный DateTimeOffset
                "datetime" or "datetimeoffset" or "timestamp" => ColumnType.DateTimeOffset,
                _ => ColumnType.Unknown
            },
            nameIndex: 1,
            typeIndex: 2,
            nullableIndex: 3,
            primaryKeyIndex: 5,
            extraInfoIndex: -1
        )
        {
        }
    }
}
