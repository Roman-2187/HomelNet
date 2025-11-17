using System.Text;
using WpfHomeNet.Data.Bilders;
using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Adapters
{
    public class SqliteSchemaAdapter : ISchemaAdapter
    {
        public string GetTableName(string rawName) =>
            ToSnakeCase(rawName);

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            // Валидация всех колонок
            foreach (var col in schema.Columns)
            {
                ValidateColumn(col);
            }

            return schema.Columns.Select(col =>
            {
                var name = $"\"{ToSnakeCase(col.Name)}\"";

                string sqlType = col.Type switch
                {
                    ColumnType.Varchar => $"TEXT",                    // SQLite: нет VARCHAR(N), используем TEXT
                    ColumnType.Integer => "INTEGER",                     // INTEGER = знаковое 64‑битное число
                    ColumnType.DateTime => "TIMESTAMP",                // поддерживается, но без точности
                    ColumnType.Boolean => "INTEGER",               // Boolean эмулируется как 0/1
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается")
                };

                var constraints = new List<string>();

                if (col.IsCreatedAt)
                {
                    // SQLite: DEFAULT CURRENT_TIMESTAMP работает только для TIMESTAMP
                    constraints.Add("DEFAULT CURRENT_TIMESTAMP");
                }

                if (!col.IsNullable)
                {
                    constraints.Add("NOT NULL");
                }

                if (col.IsPrimaryKey)
                {
                    constraints.Add("PRIMARY KEY");

                    // В SQLite PRIMARY KEY на INTEGER автоматически становится AUTOINCREMENT
                    // Но явное указание не требуется — достаточно INTEGER PRIMARY KEY
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
    }
}
