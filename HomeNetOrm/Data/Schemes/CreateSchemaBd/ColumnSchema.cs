using HomeNetOrm.Enums;


namespace HomeNetOrm.Data.Schemes.CreateSchemaBd
{

    public class ColumnSchema
    {
        public string? Name { get; set; }
        public ColumnType Type { get; set; }
        public int? Length { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }
        public bool IsAutoIncrement { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsCreatedAt { get; set; }
        public string? Comment { get; internal set; }
        public object? DefaultValue { get; set; }
        public string? OriginalName { get; set; }
        public bool IsForeignKey { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }

        /// <summary>
        /// Переопределяем метод для вывода понятного типа колонки в логах
        /// </summary>
        public override string ToString()
        {
            return Type.ToString();
        }

        /// <summary>
        /// Создает копию колонки, изменяя её имя по переданному правилу СУБД.
        /// Все остальные 15 полей копируются автоматически здесь и не мозолят глаза в адаптерах!
        /// </summary>
        public ColumnSchema CloneWithTransform(Func<string?, string?> nameTransformer)
        {
            return new ColumnSchema
            {
                // Фиксируем оригинальное C# имя для моста Dapper
                OriginalName = this.OriginalName ?? this.Name,

                // Базовое имя трансформируется по правилу конкретной СУБД
                Name = nameTransformer(this.Name),

                // Железобетонно переносим структуру
                Type = this.Type,
                Length = this.Length,
                IsNullable = this.IsNullable,
                IsPrimaryKey = this.IsPrimaryKey,
                IsUnique = this.IsUnique,
                IsAutoIncrement = this.IsAutoIncrement,
                CreatedAt = this.CreatedAt,
                IsCreatedAt = this.IsCreatedAt,
                Comment = this.Comment,
                DefaultValue = this.DefaultValue,
                IsForeignKey = this.IsForeignKey,
                ReferencedTable = this.ReferencedTable,
                ReferencedColumn = this.ReferencedColumn
            };
        }
    }
}






