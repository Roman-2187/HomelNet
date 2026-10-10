using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Interfaces;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// Спецификация типов данных для SQLite.
    /// </summary>
    public class SqliteProviderSpecification : IDbProviderSpecification
    {
        public string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length) => type switch
        {
            ColumnType.Integer => "INTEGER",
            ColumnType.Varchar => "TEXT",
            ColumnType.Boolean => "BOOLEAN",
            ColumnType.Guid => "TEXT",
            ColumnType.DateTimeOffset => "DATETIME",
            _ => "TEXT"
        };

        public ColumnType ParsePropertyType(string dbType) => dbType.ToLower() switch
        {
            "integer" => ColumnType.Integer,
            "text" or "varchar" => ColumnType.Varchar,
            "boolean" => ColumnType.Boolean,
            "datetime" or "datetimeoffset" or "timestamp" => ColumnType.DateTimeOffset,
            _ => ColumnType.Unknown
        };
    }
}
