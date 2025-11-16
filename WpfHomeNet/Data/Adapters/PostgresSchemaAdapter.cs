using System.Text;
using System.Windows.Controls;
using WpfHomeNet.Data.Bilders;
using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Adapters
{
    public class PostgresSchemaAdapter : ISchemaAdapter
    {
        public string GetTableName(string rawName) =>
            ToSnakeCase(rawName);

        public List<string> GetColumnDefinitions(TableSchema schema)
        {
            // Валидация всех колонок
            foreach (var col in schema.Columns)
            {
                ValidateColumn(col);  // Теперь передаём ColumnSchema
            }

            return schema.Columns.Select(col =>
            {
                var name = $"\"{ToSnakeCase(col.Name)}\"";

                string sqlType = col.Type switch
                {
                    ColumnType.Varchar => $"VARCHAR({col.Length})",
                    ColumnType.Int => "INTEGER",
                    ColumnType.DateTime => "TIMESTAMP",
                    ColumnType.Boolean => "BOOLEAN",
                    _ => throw new NotSupportedException($"Тип {col.Type} не поддерживается")
                };

                var constraints = new List<string>();

                if (col.IsCreatedAt) constraints.Add("DEFAULT CURRENT_TIMESTAMP");
                if (col.IsNotNull) constraints.Add("NOT NULL");
                if (col.IsPrimaryKey) constraints.Add("PRIMARY KEY");
                if (col.IsUnique) constraints.Add("UNIQUE");

                var parts = new List<string> { name, sqlType };

                if (constraints.Any())
                    parts.Add(string.Join(" ", constraints));

                return string.Join(" ", parts);
            }).ToList();
        }

        // Исправленный метод валидации
        private void ValidateColumn(ColumnSchema col)

        {
            if (col.Type == ColumnType.Unspecified)
                throw new InvalidOperationException(
                    $"Колонка '{col.Name}' " +
                    $"не имеет заданного типа. Вызовите WithDateTime()" +
                    $" или другой метод установки типа.");
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
