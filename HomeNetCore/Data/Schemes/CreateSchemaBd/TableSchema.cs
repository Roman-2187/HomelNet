namespace HomeNetCore.Data.Schemes
{
    // TableSchema с улучшенной логикой
    public class TableSchema
    { 
        public string? TableName { get; set; }
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

        public void Initialize()
        {           
           
            string idColumn = Columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name
                ?? throw new InvalidOperationException("ID-колонка не найдена в таблице");

            // Формируем все поля с алиасами (включая ID)

            columnNames = string.Join(", ",
               Columns.Select(c => $"{c.OriginalName}"));

            
               

            // БЫЛО: AllFields = string.Join(", ", Columns.Select(c => $"{c.Name} AS {c.OriginalName}"));

            // СТАЛО: Оборачиваем физическое имя в базе в кавычки "first_name" AS FirstName
            AllFields = string.Join(", ", Columns.Select(c => $"\"{c.Name}\" AS {c.OriginalName}"));

            AllParameters = string.Join(", ",
                Columns.Select(c => $"@{c.OriginalName}"));

            // Формируем поля для INSERT (исключая ID)
            InsertFields = string.Join(", ",
                Columns.Where(c => c.Name != idColumn).Select(c => c.Name));

            InsertParameters = string.Join(", ",
                Columns.Where(c => c.Name != idColumn).Select(c => $"@{c.OriginalName}"));

            // Формируем SET clause для UPDATE
            SetClause = string.Join(", ",
                Columns.Where(c => c.Name != idColumn).Select(c => $"{c.Name} = @{c.OriginalName}"));
        }


        /// <summary>
        /// Полностью переводит всю таблицу и её колонки под правила конкретной СУБД
        /// </summary>
        public TableSchema CloneWithTransform(Func<string?, string?> nameTransformer)
        {
            var transformedTable = new TableSchema
            {
                TableName = nameTransformer(this.TableName ?? string.Empty),
                Columns = this.Columns.Select(col => col.CloneWithTransform(nameTransformer)).ToList()
            };

            // Сразу запускаем пересчёт AllFields, InsertFields на новых именах
            transformedTable.Initialize();
            transformedTable.IdColumnName = transformedTable.Columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name;

            return transformedTable;
        }



    }
}
