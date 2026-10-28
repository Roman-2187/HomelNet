using System.Text;
using System.Text.RegularExpressions;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🛠️ Статический адаптер схем.
    /// Единственный турникет системы. За один проход штампует иммутабельный пакет схем и собирает дефиниции.
    /// </summary>
    internal static class GenericSchemaAdapter
    {
        private const string DefaultCurrentTimestamp = "DEFAULT CURRENT_TIMESTAMP";
        private const string NotNull = "NOT NULL";
        private const string PrimaryKey = "PRIMARY KEY";
        private const string Unique = "UNIQUE";

        // --- 🎯 ГЛАВНЫЙ ТУРНИКЕТ ДЛЯ ТРАНСФОРМАЦИИ СХЕМЫ ---

        /// <summary>
        /// Превращает сырую схему в пакет из Camel и Snake версий за один проход.
        /// </summary>
        public static AdaptedSchemas ToAdaptedSchemas(this TableSchema source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return new AdaptedSchemas
            {
                Camel = Transform(source, useSnake: false),
                Snake = Transform(source, useSnake: true)
            };
        }

        private static TableSchema Transform(TableSchema source, bool useSnake)
        {
            return new TableSchema
            {
                TableName = useSnake ? ToSnakeCase(source.TableName)! : ToCamelCase(source.TableName)!,
                Columns = [.. source.Columns.Select(col => new ColumnSchema
                {
                    Name = useSnake ? ToSnakeCase(col.Name)! : ToCamelCase(col.Name)!,
                    Type = col.Type,
                    IsPrimaryKey = col.IsPrimaryKey,
                    IsNullable = col.IsNullable,
                    IsAutoIncrement = col.IsAutoIncrement,
                    IsUnique = col.IsUnique,
                    IsCreatedAt = col.IsCreatedAt,
                    Length = col.Length,
                    DefaultValue = col.DefaultValue
                })]
            };
        }

        // --- 🛠️ МЕТОДЫ СБОРКИ ДЕФИНИЦИЙ ДЛЯ ИНИЦИАЛИЗАТОРА ---

        /// <summary>
        /// Формирует дефиниции колонок для конкретной спецификации СУБД.
        /// Доверяет регистру переданной схемы (передавать строго адаптированную схему под СУБД).
        /// </summary>
        public static List<string> GetColumnDefinitions(IDbProviderSpecification spec, TableSchema schema)
        {
            int primaryKeysCount = schema.Columns.Count(c => c.IsPrimaryKey);

            return schema.Columns.Select(col =>
            {
                if (col.Type == ColumnType.Unspecified)
                    throw new InvalidOperationException($"Колонка '{col.Name}' не имеет заданного типа.");

                // 🔥 ФИКС: Больше никакой самодеятельности с конвертацией регистров на лету!
                // Имя берется "как есть" из схемы, потому что схема уже прошла через ToAdaptedSchemas()
                var name = $"\"{col.Name}\"";

                string sqlType = spec.MapToSqlType(col.Type, col.IsPrimaryKey, col.IsAutoIncrement, col.Length);
                var constraints = new List<string>();

                if (col.DefaultValue != null)
                {
                    string defaultValue = col.Type switch
                    {
                        ColumnType.Varchar or ColumnType.DateTime => $"'{col.DefaultValue}'",
                        _ => col.DefaultValue.ToString() ?? throw new InvalidOperationException()
                    };
                    constraints.Add($"DEFAULT {defaultValue}");
                }
                else if (col.IsCreatedAt)
                {
                    constraints.Add(DefaultCurrentTimestamp);
                }

                if (!col.IsNullable) constraints.Add(NotNull);

                if (col.IsPrimaryKey && primaryKeysCount == 1)
                    constraints.Add(PrimaryKey);

                if (col.IsPrimaryKey && col.IsAutoIncrement && sqlType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    constraints.Add("AUTOINCREMENT");
                }

                if (col.IsUnique) constraints.Add(Unique);

                var parts = new List<string> { name, sqlType };
                if (constraints.Any()) parts.Add(string.Join(" ", constraints));

                return string.Join(" ", parts);
            }).ToList();
        }

        // --- ВНУТРЕННИЕ ПРИВАТНЫЕ КИШКИ ТРАНСФОРМАЦИИ СТРОК ---

        public static string? ToSnakeCase(string? name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) builder.Append('_');
                    builder.Append(char.ToLower(c));
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        private static string? ToCamelCase(string? name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            if (!name.Contains('_') && char.IsUpper(name[0]))
                return name;

            string safeName = char.ToUpper(name[0]) + name.Substring(1);

            return Regex.Replace(
                safeName,
                @"_([a-zA-Z])",
                match => match.Groups[1].Value.ToUpper()
            ).Replace("_", "");
        }
    }
}
