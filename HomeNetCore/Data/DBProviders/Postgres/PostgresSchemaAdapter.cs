using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using System.Text;

namespace WpfHomeNet.Data.DBProviders.Postgres

{
    public class PostgresSchemaAdapter : ISchemaAdapter
    {
        public string ConvertTableName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                throw new ArgumentException("Имя таблицы не может быть пустым");
            }

            return ToSnakeCase(rawName);
        }

        public string ConvertColumnName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                throw new ArgumentException("Имя колонки не может быть пустым");
            }

            if (rawName.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException("Имя колонки не должно содержать пробелы");
            }

            return ToSnakeCase(rawName);
        }

        public TableSchema ConvertToSnakeCaseSchema(TableSchema originalSchema)
        {
            var snakeCaseSchema = new TableSchema
            {
                TableName = ConvertTableName(originalSchema.TableName, NameFormat.SnakeCase),
                Columns = originalSchema.Columns.Select(col =>
                    new ColumnSchema
                    {
                        OriginalName = col.Name,
                        Name = ConvertColumnName(col.Name, NameFormat.SnakeCase),
                        Type = col.Type,
                        Length = col.Length,
                        IsNullable = col.IsNullable,
                        IsPrimaryKey = col.IsPrimaryKey,
                        IsUnique = col.IsUnique,
                        IsAutoIncrement = col.IsAutoIncrement,
                        CreatedAt = col.CreatedAt,
                        IsCreatedAt = col.IsCreatedAt,
                        Comment = col.Comment,
                        DefaultValue = col.DefaultValue
                    }).ToList()
            };
            snakeCaseSchema.Initialize();

            // Теперь присваиваем IdColumnName на основе преобразованной схемы
            snakeCaseSchema.IdColumnName = snakeCaseSchema.Columns
                .FirstOrDefault(c => c.IsPrimaryKey)?.Name;

            return snakeCaseSchema;
        }

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            foreach (var col in schema.Columns)
            {
                ValidateColumn(col);
            }

            return schema.Columns.Select(col =>
            {
                var name = $"\"{ConvertColumnName(col.Name, NameFormat.SnakeCase)}\"";

                string sqlType = col.Type switch
                {
                    ColumnType.Varchar => col.Length.HasValue ? $"VARCHAR({col.Length})" : "VARCHAR",
                    ColumnType.Integer => "INTEGER",
                    ColumnType.DateTime => "TIMESTAMP",
                    ColumnType.Boolean => "BOOLEAN",                                        
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается")
                };

                var constraints = new List<string>();

                // Обработка DefaultValue с учетом типа
                if (col.DefaultValue != null)
                {
                    string defaultValue;

                    switch (col.Type)
                    {
                        case ColumnType.Varchar:
                        
                            defaultValue = $"'{col.DefaultValue}'";  // Строки в кавычках
                            break;
                        case ColumnType.DateTime:
                            defaultValue = $"'{col.DefaultValue}'";  // Даты в кавычках
                            break;
                        case ColumnType.Integer:
                        case ColumnType.Boolean:
                            defaultValue = col.DefaultValue.ToString();  // Числа и булевы без кавычек
                            break;
                        default:
                            defaultValue = $"'{col.DefaultValue}'";
                            break;
                    }

                    constraints.Add($"DEFAULT {defaultValue}");
                }
                else if (col.IsCreatedAt)
                {
                    constraints.Add("DEFAULT CURRENT_TIMESTAMP");
                }

                if (!col.IsNullable)
                {
                    constraints.Add("NOT NULL");
                }

                if (col.IsPrimaryKey)
                {
                    constraints.Add("PRIMARY KEY");
                }

                if (col.IsUnique)
                {
                    constraints.Add("UNIQUE");
                }

                var parts = new List<string> { name, sqlType };

                if (constraints.Any())
                    parts.Add(string.Join(" ", constraints));

                return string.Join(" ", parts);
            }).ToList();
        }


        private void ValidateColumn(ColumnSchema col)
        {
            if (col.Type == ColumnType.Unspecified)
                throw new InvalidOperationException(
                    $"Колонка '{col.Name}' не имеет заданного типа. " +
                    "Вызовите метод установки типа.");
        }

        private string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var builder = new StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        builder.Append('_');
                    builder.Append(char.ToLower(c));
                }
                else if (char.IsWhiteSpace(c))
                {
                    builder.Append('_');  // Замена пробелов на подчеркивания
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        // Добавим методы для создания индексов и ограничений
        //public string CreateIndex(string tableName, string columnName)
        //{
        //    return $"CREATE INDEX idx_{ToSnakeCase(tableName)}_{ToSnakeCase(columnName)} " +
        //           $"ON \"{ConvertTableName(tableName)}\"(\"{GetColumnName(columnName)}\")";
        //}

        //public string CreateForeignKey(string tableName, string columnName, string referencedTable, string referencedColumn)
        //{
        //    return $"ALTER TABLE \"{GetTableName(tableName)}\" " +
        //           $"ADD CONSTRAINT fk_{ToSnakeCase(tableName)}_{ToSnakeCase(columnName)} " +
        //           $"FOREIGN KEY (\"{GetColumnName(columnName)}\") " +
        //           $"REFERENCES \"{GetTableName(referencedTable)}\"(\"{GetColumnName(referencedColumn)}\")";
        //}

        //// Метод для создания таблицы
        //public string CreateTable(TableSchema schema)
        //{
        //    var columns = GetColumnDefinitions(schema);
        //    var columnDefinitions = string.Join(",\n    ", columns);

        //    return $"CREATE TABLE \"{GetTableName(schema.TableName)}\" (\n    {columnDefinitions}\n)";
        //}
    }
}
