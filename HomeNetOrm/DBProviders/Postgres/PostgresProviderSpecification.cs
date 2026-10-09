using System;
using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Extensions; // Твой класс спецификации

namespace HomeNetOrm.DBProviders.Postgres
{
    /// <summary>
    /// Паспорт спецификации типов данных и индексов маппинга для PostgreSQL.
    /// </summary>
    public class PostgresProviderSpecification : DbProviderSpecificationExtensions
    {
        public PostgresProviderSpecification() : base(
            tableExistsQuery: string.Empty,
            getTableStructureQuery: string.Empty,

           mapToSqlType: (type, isPk, isAi, length) => type switch
           {
               ColumnType.Integer => isPk && isAi ? "SERIAL" : "INTEGER",
               ColumnType.Varchar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR",
               ColumnType.Boolean => "BOOLEAN",
               ColumnType.Guid => "UUID",

               // 🔥 ВОЗВРАЩАЕМ РОДНУЮ ТАЙМЗОНУ: Теперь Postgres хранит глобальное время со смещением!
               ColumnType.DateTimeOffset => "TIMESTAMPTZ",
               _ => "VARCHAR"
           },
            parsePropertyType: dbType => dbType.ToLower() switch
            {
                "integer" or "serial" => ColumnType.Integer,
                "character varying" or "varchar" or "text" => ColumnType.Varchar,
                "uuid" => ColumnType.Guid,
                "boolean" => ColumnType.Boolean,

                // ⚡ ЧИТАЕМ СТРУКТУРУ: Любые типы штампов времени из Postgres приводим к DateTimeOffset
                "timestamp without time zone" or "timestamp" or "timestamp with time zone" or "timestamptz" => ColumnType.DateTimeOffset,
                _ => ColumnType.Unknown
            },

            nameIndex: 0,
            typeIndex: 1,
            nullableIndex: 3,
            primaryKeyIndex: 4,
            extraInfoIndex: 5
        )
        {
        }
    }
}
