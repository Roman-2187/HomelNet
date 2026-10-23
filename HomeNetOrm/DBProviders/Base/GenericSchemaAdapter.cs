using System.Text;
using System.Text.RegularExpressions;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🛠️ Статический адаптер схем.
    /// Отвечает за маппинг типов и имен колонок без выделения лишней памяти и конструкторов.
    /// </summary>
    internal static class GenericSchemaAdapter
    {
        private const string DefaultCurrentTimestamp = "DEFAULT CURRENT_TIMESTAMP";
        private const string NotNull = "NOT NULL";
        private const string PrimaryKey = "PRIMARY KEY";
        private const string Unique = "UNIQUE";

        // --- ПУБЛИЧНЫЕ (INTERNAL) МЕТОДЫ РАСШИРЕНИЯ ДЛЯ ВНЕШНЕГО КОДА ---

        public static string ConvertTableName(this string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя таблицы не может быть пустым");
            return format == NameFormat.SnakeCase ? ToSnakeCase(rawName)! : ToCamelCase(rawName)!;
        }

        public static string ConvertColumnName(this string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя колонки не может быть пустым");
            return format == NameFormat.SnakeCase ? ToSnakeCase(rawName)! : ToCamelCase(rawName)!;
        }

        /// <summary>
        /// Формирует дефиниции колонок для конкретной спецификации СУБД.
        /// </summary>
        public static List<string> GetColumnDefinitions(IDbProviderSpecification spec, TableSchema schema)
        {
            int primaryKeysCount = schema.Columns.Count(c => c.IsPrimaryKey);

            return schema.Columns.Select(col =>
            {
                if (col.Type == ColumnType.Unspecified)
                    throw new InvalidOperationException($"Колонка '{col.Name}' не имеет заданного типа.");

                // Трансформируем имя прямо на месте через наши внутренние методы
                string dbColumnName = col.Name.ConvertColumnName(spec.TableFormat);
                var name = $"\"{dbColumnName}\"";

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


        // --- 🔥 ЗАКРЫТЫЕ ВНУТРЕННИЕ КИШКИ ТРАНСФОРМАЦИИ СТРОК ---

        private static string? ToSnakeCase(string? name)
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
