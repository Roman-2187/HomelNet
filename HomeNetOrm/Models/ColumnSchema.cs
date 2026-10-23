using HomeNetOrm.Enums;
using System;

namespace HomeNetOrm.Models
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
        public bool IsForeignKey { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }

        public ColumnSchema() { }

        // 🔥 ЧИСТЫЙ КОНСТРУКТОР: Принимает метаданные и заполняет свойства
        public ColumnSchema(RawColumnMetadata metadata, Func<string, ColumnType> typeParser)
        {
            Name = metadata.Name;
            Type = typeParser(metadata.DataType);
            IsNullable = metadata.IsNullable;
            IsPrimaryKey = string.Equals(metadata.KeyType, "YES", StringComparison.OrdinalIgnoreCase);

            IsAutoIncrement = metadata.ExtraInfo != null &&
                             (metadata.ExtraInfo.Contains("nextval") ||
                              metadata.ExtraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString() => $"{Name} ({Type})";
    }
}
