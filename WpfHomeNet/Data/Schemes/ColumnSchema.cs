using WpfHomeNet.Data.Bilders;

namespace WpfHomeNet.Data.Schemes
{
    using System;

    public class ColumnSchema
    {
        public string? Name { get; set; }
        public ColumnType Type { get; set; }
        public int? Length { get; set; }
        /// <summary>
        /// true — колонка допускает значение NULL (в SQL: ... NULL).
        /// false — колонка не допускает NULL (в SQL: ... NOT NULL).
        /// Соответствует полю information_schema.columns.is_nullable.
        /// </summary>
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }  // Новое свойство
        public bool IsAutoIncrement { get; set; }
        public DateTime? CreatedAt { get; set; }  // Новое свойство
        public bool IsCreatedAt { get; set; }  // true, если колонка — «время создания»
        public string? Comment { get; internal set; }
        public object? DefaultValue { get; internal set; }
    }
}





