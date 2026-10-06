namespace HomeNetOrm.Models
{
    // TableSchema с улучшенной логикой
    public class TableSchema
    { 
        

        public string TableName { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new();

        // Все поля для общих операций с алиасами
        public string? AllFields { get; set; }
        public string? AllParameters { get; set; }

        //
        /// <summary>
        ///  Поля для INSERT (без ID)
        /// </summary>
        public string? InsertFields { get; set; }

        /// <summary>
        /// параметры для INSERT (без ID)
        /// </summary>
        public string? InsertParameters { get; set; }

        
        /// <summary>
        /// SET clause для UPDATE
        /// </summary>
        public string? SetClause { get; set; }

        /// <summary>
        /// Находим ID-колонку автоматически по IsPrimaryKey в методе Initialize();
        /// </summary>
        public string? IdColumnName { get; set; }

        public string? columnNames { get; set; }

        public bool Initialize()
        {
            var pkColumn = Columns.FirstOrDefault(c => c.IsPrimaryKey);
            if (pkColumn == null)
            {
                return false; // Фейсконтроль по PK не пройден
            }

            string idColumn = pkColumn.Name ?? "Null";
            IdColumnName = idColumn;

            columnNames = string.Join(", ", Columns.Select(c => $"{c.OriginalName}"));

            // Для выборки (SELECT) подтягиваем все поля с алиасами
            AllFields = string.Join(", ", Columns.Select(c => $"\"{c.Name}\" AS {c.OriginalName}"));
            AllParameters = string.Join(", ", Columns.Select(c => $"@{c.OriginalName}"));

            // 🎯 ЖЕЛЕЗОБЕТОННЫЙ ФИКС ДЛЯ GUID ИНСЕРТОВ:
            // Исключаем колонку из INSERT только если для нее включен автоинкремент базы!
            // Наш C#-овский Guid (у которого IsAutoIncrement = false) теперь честно попадет в запрос.
            var insertColumns = Columns.Where(c => !c.IsAutoIncrement).ToList();

            InsertFields = string.Join(", ", insertColumns.Select(c => $"\"{c.Name}\""));
            InsertParameters = string.Join(", ", insertColumns.Select(c => $"@{c.OriginalName}"));

            // Для UPDATE блок SET по-прежнему должен исключать первичный ключ (его менять нельзя)
            SetClause = string.Join(", ", Columns
                .Where(c => !string.Equals(c.Name, idColumn, StringComparison.OrdinalIgnoreCase))
                .Select(c => $"\"{c.Name}\" = @{c.OriginalName}"));

            return true;
        }





        /// <summary>
        /// Полностью переводит всю таблицу и её колонки под правила конкретной СУБД
        /// </summary>
        public TableSchema CloneWithTransform(Func<string?, string?> nameTransformer)
        {
            var transformedTable = new TableSchema
            {
                // 1. Берем имя (если null, то пустую строку) и ставим "!" в конце, чтобы компилятор не ворчал
                TableName = nameTransformer(this.TableName ?? string.Empty)!,

                // 2. Добавляем проверку на null для коллекции Columns на всякий случай
                Columns = this.Columns?.Select(c => c.CloneWithTransform(nameTransformer)).ToList() ?? []};
        
            

            // Сразу запускаем пересчёт AllFields, InsertFields на новых именах
            transformedTable.Initialize();
            transformedTable.IdColumnName = transformedTable.Columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name;

            return transformedTable;
        }



    }
}
