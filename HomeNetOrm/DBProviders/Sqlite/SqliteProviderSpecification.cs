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
                ColumnType.DateTime => "DATETIME",
                ColumnType.Boolean => "BOOLEAN",
                ColumnType.Guid => "TEXT",
                ColumnType.DateTimeOffset => "DATETIME", // Наш Dapper-фикс
                _ => "TEXT"
            },
            parsePropertyType: dbType => dbType.ToLower() switch
            {
                "integer" => ColumnType.Integer,
                "text" or "varchar" => ColumnType.Varchar,
                "datetime" or "datetimeoffset" => ColumnType.DateTimeOffset,
                "boolean" => ColumnType.Boolean,
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
