using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// Спецификация типов данных для SQLite.
    /// </summary>
    internal class SqliteProviderSpecification : IDbProviderSpecification
    {
        // 🔥 Указываем формат именования для SQLite
        public NameFormat TableFormat => NameFormat.CamelCase;

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

        public string? GetColumnFeatures(ColumnType type, bool isPk, bool isAi, object? defaultValue, bool isCreatedAt)
        {
            var features = new List<string>();

            if (defaultValue != null)
            {
                string valueStr = (type == ColumnType.Varchar || type == ColumnType.DateTimeOffset) ? $"'{defaultValue}'" : defaultValue.ToString()!;
                features.Add($"DEFAULT {valueStr}");
            }
            else if (isCreatedAt)
            {
                features.Add("DEFAULT CURRENT_TIMESTAMP");
            }

            // SQLite требует слово AUTOINCREMENT строго инлайново для INTEGER PRIMARY KEY
            if (isPk && isAi && type == ColumnType.Integer)
            {
                features.Add("AUTOINCREMENT");
            }

            return features.Any() ? string.Join(" ", features) : null;
        }

    }
}
