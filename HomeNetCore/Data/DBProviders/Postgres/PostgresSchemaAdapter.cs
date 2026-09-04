using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers; // Используем наш единый статик класс со StringExtensions 🚀
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfHomeNet.Data.DBProviders.Postgres
{
    public class PostgresSchemaAdapter : ISchemaAdapter
    {
        private const string DefaultCurrentTimestamp = "DEFAULT CURRENT_TIMESTAMP";
        private const string NotNull = "NOT NULL";
        private const string PrimaryKey = "PRIMARY KEY";
        private const string Unique = "UNIQUE";

        public string ConvertTableName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя таблицы не может быть пустым");
            return format == NameFormat.SnakeCase ? rawName.ToSnakeCase()! : rawName.ToCamelCase()!;
        }

        public string ConvertColumnName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName)) throw new ArgumentException("Имя колонки не может быть пустым");
            if (rawName.Any(char.IsWhiteSpace)) throw new ArgumentException("Имя колонки не должно содержать пробелы");
            return format == NameFormat.SnakeCase ? rawName.ToSnakeCase()! : rawName.ToCamelCase()!;
        }

        /// <summary>
        /// Вот ОНО! Теперь и для Postgres метод сжался до одной строчки. 
        /// Инкапсуляция на полную мощность! 🧙‍♂️💎
        /// </summary>
        public TableSchema ConvertToSnakeCaseSchema(TableSchema originalSchema)
        {
            return originalSchema.CloneWithTransform(name => name.ToSnakeCase());
        }

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            foreach (var col in schema.Columns)
            {
                ValidateColumn(col);
            }

            return schema.Columns.Select(col =>
            {
                // Оборачиваем имя в экранирующие кавычки под стандарты Postgres
                var name = $"\"{col.Name}\"";

                string sqlType = col.Type switch
                {
                    // Postgres требует явного указания автоинкремента через SERIAL!
                    ColumnType.Integer => col.IsPrimaryKey && col.IsAutoIncrement ? "SERIAL" : "INTEGER",
                    ColumnType.Varchar => col.Length.HasValue ? $"VARCHAR({col.Length})" : "VARCHAR",
                    ColumnType.DateTime => "TIMESTAMP",
                    ColumnType.Boolean => "BOOLEAN",
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается Postgres")
                };

                var constraints = new List<string>();

                // Если это SERIAL (первичный ключ с автоинкрементом в Postgres), 
                // то ключевые слова PRIMARY KEY и NOT NULL добавляются как обычно, но дефолтное значение SERIAL генерирует сам!
                if (col.DefaultValue != null)
                {
                    string defaultValue = col.Type switch
                    {
                        ColumnType.Varchar or ColumnType.DateTime => $"'{col.DefaultValue}'",
                        ColumnType.Integer or ColumnType.Boolean => col.DefaultValue.ToString()
                            ?? throw new InvalidOperationException($"Некорректное дефолтное значение для колонки {col.Name}"),
                        _ => $"'{col.DefaultValue}'"
                    };
                    constraints.Add($"DEFAULT {defaultValue}");
                }
                else if (col.IsCreatedAt)
                {
                    constraints.Add(DefaultCurrentTimestamp);
                }

                if (!col.IsNullable) constraints.Add(NotNull);
                if (col.IsPrimaryKey) constraints.Add(PrimaryKey);
                if (col.IsUnique) constraints.Add(Unique);

                var parts = new List<string> { name, sqlType };
                if (constraints.Any()) parts.Add(string.Join(" ", constraints));

                return string.Join(" ", parts);
            }).ToList();
        }

        private void ValidateColumn(ColumnSchema col)
        {
            if (col.Type == ColumnType.Unspecified)
                throw new InvalidOperationException($"Колонка '{col.Name}' не имеет заданного типа.");
        }
    }
}

