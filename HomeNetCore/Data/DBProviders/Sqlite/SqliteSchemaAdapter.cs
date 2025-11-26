using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Schemes;
using System.Text;
using System.Text.RegularExpressions;

namespace WpfHomeNet.Data.DBProviders.SqliteClasses
{
    public class SqliteSchemaAdapter : ISchemaAdapter
    {
        public string ConvertTableName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                throw new ArgumentException("Имя таблицы не может быть пустым");
            }

            return format switch
            {
                NameFormat.SnakeCase => ToSnakeCase(rawName),
                NameFormat.CamelCase => ToCamelCase(rawName),
                _ => throw new ArgumentException("Неизвестный формат")
            };
        }
       
        public string ConvertColumnName(string? rawName, NameFormat format)
        {
            if (string.IsNullOrEmpty(rawName))
                throw new ArgumentException("Имя колонки не может быть пустым");

            if (rawName.Any(char.IsWhiteSpace))
                throw new ArgumentException("Имя колонки не должно содержать пробелы");

            return format switch
            {
                NameFormat.SnakeCase => ToSnakeCase(rawName),
                NameFormat.CamelCase => ToCamelCase(rawName),
                _ => throw new ArgumentException("Неизвестный формат")
            };          
        }


        /// <summary>
        /// Преобразует схему в формат snake_case для использования в SQL-запросах
        /// </summary>
        /// <param name="originalSchema">Исходная схема таблицы</param>
        /// <returns>Отформатированная схема с именами в snake_case</returns>
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

            // Сначала инициализируем схему
            snakeCaseSchema.Initialize();

            // Теперь присваиваем IdColumnName на основе преобразованной схемы
            snakeCaseSchema.IdColumnName = snakeCaseSchema.Columns
                .FirstOrDefault(c => c.IsPrimaryKey)?.Name;

            return snakeCaseSchema;
        }


        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            // Валидация всех колонок
            foreach (var col in schema.Columns)
            {
                ValidateColumn(col);
            }

            return schema.Columns.Select(col =>
            {
                var name = $"\"{ConvertColumnName(col.Name,NameFormat.SnakeCase)}\"";

                string sqlType = col.Type switch
                {
                    ColumnType.Varchar => "TEXT",
                    ColumnType.Integer => "INTEGER",
                    ColumnType.DateTime => "TIMESTAMP",
                    ColumnType.Boolean => "INTEGER",
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается")
                };

                var constraints = new List<string>();

                if (col.IsCreatedAt)
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

                if (col.IsAutoIncrement)
                {
                    constraints.Add("AUTOINCREMENT");
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
                    $"Колонка '{col.Name}' " +
                    $"не имеет заданного типа. Вызовите WithDateTime() " +
                    $"или другой метод установки типа.");
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
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;


            if (!name.Contains('_') && char.IsUpper(name[0]))
                return name;

                // Проверяем, есть ли подчёркивания
                if (name.Contains('_'))
            {
                // Преобразуем первую букву в нижний регистр (для camelCase)
                name = char.ToUpper(name[0]) + name.Substring(1);
            }
            else
            {
                // Если нет подчёркиваний - делаем первую букву заглавной
                name = char.ToUpper(name[0]) + name.Substring(1);
            }

            // Применяем регулярное выражение для обработки подчёркиваний
            return Regex.Replace(
                name,
                @"_([a-zA-Z])",
                match => match.Groups[1].Value.ToUpper()
            ).Replace("_", "");
        }   
    }
}
