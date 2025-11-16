using WpfHomeNet.Data.Bilders;

namespace WpfHomeNet.Data.Schemes
{
    using System;

    public class ColumnSchema
    {
        public string? Name { get; set; }
        public ColumnType Type { get; set; }
        public int? Length { get; set; }
        public bool IsNotNull { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }  // Новое свойство
        public bool IsAutoIncrement { get; set; }
        public DateTime? CreatedAt { get; set; }  // Новое свойство
        public bool IsCreatedAt { get; set; }  // true, если колонка — «время создания»
        public string? Comment { get; internal set; }

    }
}





