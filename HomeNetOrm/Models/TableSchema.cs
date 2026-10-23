namespace HomeNetOrm.Models
{
    public class TableSchema
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new();

        // Все поля для SELECT-операций с алиасами
        public string? AllFields { get; set; }
        public string? AllParameters { get; set; }

        // Поля и параметры для INSERT (исключая автоинкремент)
        public string? InsertFields { get; set; }
        public string? InsertParameters { get; set; }

        // SET clause для UPDATE (исключает ВСЕ колонки первичного ключа)
        public string? SetClause { get; set; }

        // Для обратной совместимости: имя одиночного ПК (null, если ключ составной)
        public string? IdColumnName { get; set; }

        // Имена колонок через запятую (чистые, без алиасов)
        public string? ColumnNames { get; set; }

        // 🔥 ДОБАВЛЯЕМ: Флаг для серверного триггера авто-инкремента версий
        public bool UseVersionTrigger { get; set; } = false;

        public bool Initialize()
        {
            // Находим все колонки, помеченные как Primary Key
            var pkColumns = Columns.Where(c => c.IsPrimaryKey).ToList();
            if (!pkColumns.Any())
            {
                return false; // Таблица без PK — это не по правилам распределенных баз
            }

            // Записываем одиночный ID (только если он один, иначе null для защиты)
            IdColumnName = pkColumns.Count == 1 ? pkColumns.First().Name : null;

            // Чистый список колонок базы данных
            ColumnNames = string.Join(", ", Columns.Select(c => $"\"{c.Name}\""));

            // Для выборки (SELECT) подтягиваем все поля с алиасами под C#-свойства
            AllFields = string.Join(", ", Columns.Select(c => $"\"{c.Name}\" AS {c.OriginalName}"));
            AllParameters = string.Join(", ", Columns.Select(c => $"@{c.OriginalName}"));

            // Исключаем из INSERT только автоинкрементные поля. Guid и обычные типы летят в запрос
            var insertColumns = Columns.Where(c => !c.IsAutoIncrement).ToList();
            InsertFields = string.Join(", ", insertColumns.Select(c => $"\"{c.Name}\""));
            InsertParameters = string.Join(", ", insertColumns.Select(c => $"@{c.OriginalName}"));

            // 🎯 СВЕРХНАДЕЖНЫЙ ФИКС ДЛЯ СОСТАВНЫХ КЛЮЧЕЙ:
            // В блоке SET для UPDATE мы полностью исключаем абсолютно ВСЕ колонки, входящие в PK!
            SetClause = string.Join(", ", Columns
                .Where(c => !c.IsPrimaryKey)
                .Select(c => $"\"{c.Name}\" = @{c.OriginalName}"));

            return true;
        }

        /// <summary>
        /// Полностью переводит всю таблицу и её колонки под правила конкретной СУБД (Postgres/SQLite)
        /// </summary>
        public TableSchema CloneWithTransform(Func<string?, string?> nameTransformer)
        {
            var transformedTable = new TableSchema
            {
                TableName = nameTransformer(this.TableName ?? string.Empty)!,
                Columns = this.Columns?.Select(c => c.CloneWithTransform(nameTransformer)).ToList() ?? new List<ColumnSchema>(),
                UseVersionTrigger = this.UseVersionTrigger // Не теряем флаг при клонировании!
            };

            // Пересчитываем SQL-блоки на трансформированных (например, нижнерегистровых) именах
            transformedTable.Initialize();

            return transformedTable;
        }
    }
}
