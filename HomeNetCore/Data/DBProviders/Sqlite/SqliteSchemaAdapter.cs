using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers; // Наш статик класс со StringExtensions
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    public class SqliteSchemaAdapter : ISchemaAdapter
    {
        private const string TypeText = "TEXT";
        private const string TypeInteger = "INTEGER";
        private const string TypeTimestamp = "TIMESTAMP";
        private const string TypeReal = "REAL";
        private const string DefaultCurrentTimestamp = "DEFAULT CURRENT_TIMESTAMP";
        private const string NotNull = "NOT NULL";
        private const string PrimaryKey = "PRIMARY KEY";
        private const string Unique = "UNIQUE";
        private const string AutoIncrement = "AUTOINCREMENT";

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
        /// <summary>
        /// Вот ОНО! Метод сжался до одной строчки. Схема сама всё перекладывает внутри себя!
        /// </summary>
        public TableSchema ConvertToSnakeCaseSchema(TableSchema originalSchema)
        {
            return originalSchema.CloneWithTransform(name => name.ToSnakeCase());
        }

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            var definitions = schema.Columns.Select(col =>
            {
                var name = $"\"{col.Name}\"";

                string sqlType = col.Type switch
                {
                    ColumnType.Varchar => TypeText,
                    ColumnType.Integer => TypeInteger,
                    ColumnType.DateTime => TypeTimestamp,
                    ColumnType.Boolean => TypeInteger,
                    ColumnType.Real => TypeReal,
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается SQLite")
                };

                var constraints = new List<string>();

                if (col.IsCreatedAt) constraints.Add(DefaultCurrentTimestamp);
                if (!col.IsNullable) constraints.Add(NotNull);
                if (col.IsPrimaryKey) constraints.Add(PrimaryKey);
                if (col.IsUnique) constraints.Add(Unique);
                if (col.IsAutoIncrement) constraints.Add(AutoIncrement);

                var parts = new List<string> { name, sqlType };
                if (constraints.Any()) parts.Add(string.Join(" ", constraints));

                return string.Join(" ", parts);
            }).ToList();

            foreach (var col in schema.Columns.Where(c => c.IsForeignKey))
            {
                definitions.Add($"FOREIGN KEY(\"{col.Name}\") REFERENCES \"" +
                    $"{col.ReferencedTable.ToSnakeCase()}\"(\"" +
                    $"{col.ReferencedColumn.ToSnakeCase()}\") ON DELETE RESTRICT");
            }

            return definitions;
        }
    }
}
