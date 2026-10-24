using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;

namespace HomeNetOrm.DBProviders.Postgres.@base
{
    /// <summary>
    /// Спецификация типов данных для PostgreSQL.
    /// </summary>
    internal class PostgresProviderSpecification : IDbProviderSpecification
    {
        // 🔥 Указываем строгий snake_case регистр для Postgres
        public NameFormat TableFormat => NameFormat.SnakeCase;

        public string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length)
        {
            // Если в Postgres идет автоинкрементный ключ, его тип объявляется как SERIAL или BIGSERIAL
            if (isPk && isAi && type == ColumnType.Integer) return "SERIAL";

            return type switch
            {
                ColumnType.Integer => "INTEGER",
                ColumnType.Varchar => length.HasValue ? $"VARCHAR({length})" : "TEXT",
                ColumnType.Boolean => "BOOLEAN",
                ColumnType.Guid => "UUID",
                ColumnType.DateTimeOffset => "TIMESTAMP WITH TIME ZONE",
                _ => "TEXT"
            };
        }

        public ColumnType ParsePropertyType(string dbType) => dbType.ToLower() switch
        {
            "integer" or "numeric" => ColumnType.Integer,
            "text" or "character varying" or "varchar" => ColumnType.Varchar,
            "boolean" => ColumnType.Boolean,
            "uuid" => ColumnType.Guid,
            "timestamp with time zone" or "timestamptz" => ColumnType.DateTimeOffset,
            _ => ColumnType.Unknown
        };

        public string? GetColumnFeatures(ColumnType type, bool isPk, bool isAi, object? defaultValue, bool isCreatedAt)
        {
            var features = new List<string>();

            if (defaultValue != null)
            {
                string valueStr = (type == ColumnType.Varchar || type == ColumnType.DateTimeOffset || type == ColumnType.Guid) ? $"'{defaultValue}'" : defaultValue.ToString()!;
                features.Add($"DEFAULT {valueStr}");
            }
            else if (isCreatedAt)
            {
                features.Add("DEFAULT CURRENT_TIMESTAMP");
            }

            return features.Any() ? string.Join(" ", features) : null;
        }

    }
}
