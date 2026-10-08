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
               // 🎯 ИСПРАВЛЕНО: переводим на обычный TIMESTAMP, чтобы типы совпали с SQLite!
               ColumnType.DateTime => "TIMESTAMP",
               ColumnType.DateTimeOffset => "TIMESTAMP",
               ColumnType.Boolean => "BOOLEAN",
               ColumnType.Guid => "UUID",
               _ => "VARCHAR"
           },
parsePropertyType: dbType => dbType.ToLower() switch
{
    "integer" or "serial" => ColumnType.Integer,
    "character varying" or "varchar" or "text" => ColumnType.Varchar,
    // 🎯 ИСПРАВЛЕНО: ловим TIMESTAMP как ColumnType.DateTime
    "timestamp without time zone" or "timestamp" => ColumnType.DateTime,
    "timestamp with time zone" or "timestamptz" => ColumnType.DateTime,
    "uuid" => ColumnType.Guid,
    "boolean" => ColumnType.Boolean,
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
