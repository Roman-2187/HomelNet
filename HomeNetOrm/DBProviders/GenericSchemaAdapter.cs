using System;
using System.Collections.Generic;
using System.Linq;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders
{
    public class GenericSchemaAdapter : ISchemaAdapter
    {
        private const string DefaultCurrentTimestamp = "DEFAULT CURRENT_TIMESTAMP";
        private const string NotNull = "NOT NULL";
        private const string PrimaryKey = "PRIMARY KEY";
        private const string Unique = "UNIQUE";

        // 🔥 СТАЛО ЧИСТО: Одна строгая, понятная модель конфигурации вместо кучи полей!
        public DbProviderSpecification Spec { get; }

        public GenericSchemaAdapter(DbProviderSpecification spec)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        public string ConvertTableName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя таблицы не может быть пустым");
            return format == NameFormat.SnakeCase ? rawName.ToSnakeCase()! : rawName.ToCamelCase()!;
        }

        public string ConvertColumnName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя колонки не может быть пустым");
            return format == NameFormat.SnakeCase ? rawName.ToSnakeCase()! : rawName.ToCamelCase()!;
        }

        public TableSchema ConvertToSnakeCaseSchema(TableSchema originalSchema)
        {
            return originalSchema.CloneWithTransform(name => name.ToSnakeCase());
        }

        // Пробрасываем вызов парсера типов напрямую из нашей спецификации
        public ColumnType MapDbSpecificationType(string dbType) => Spec.DbTypeParser(dbType);

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            return schema.Columns.Select(col =>
            {
                if (col.Type == ColumnType.Unspecified)
                    throw new InvalidOperationException($"Колонка '{col.Name}' не имеет заданного типа.");

                var name = $"\"{col.Name}\"";

                // 🔥 Вызываем делегат трансляции SQL-типов напрямую через объект Spec!
                string sqlType = Spec.TypeMapper(col.Type, col.IsPrimaryKey, col.IsAutoIncrement, col.Length);

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
                if (col.IsPrimaryKey) constraints.Add(PrimaryKey);

                // Специфика SQLite для автоинкремента:
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
    }
}
