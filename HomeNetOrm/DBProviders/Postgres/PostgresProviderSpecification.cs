using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Interfaces;

namespace HomeNetOrm.DBProviders.Postgres
{
    /// <summary>
    /// Спецификация типов данных для PostgreSQL.
    /// </summary>
    public class PostgresProviderSpecification : IDbProviderSpecification
    {
        public string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length) => type switch
        {
            ColumnType.Integer => isPk && isAi ? "SERIAL" : "INTEGER",
            ColumnType.Varchar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR",
            ColumnType.Boolean => "BOOLEAN",
            ColumnType.Guid => "UUID",
            ColumnType.DateTimeOffset => "TIMESTAMPTZ",
            _ => "VARCHAR"
        };

        public ColumnType ParsePropertyType(string dbType) => dbType.ToLower() switch
        {
            "integer" or "serial" => ColumnType.Integer,
            "character varying" or "varchar" or "text" => ColumnType.Varchar,
            "uuid" => ColumnType.Guid,
            "boolean" => ColumnType.Boolean,
            "timestamp without time zone" or "timestamp" or "timestamp with time zone" or "timestamptz" => ColumnType.DateTimeOffset,
            _ => ColumnType.Unknown
        };
    }
}
