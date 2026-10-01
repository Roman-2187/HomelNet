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
        public string? OriginalName { get; set; }
        public bool IsForeignKey { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }

        public ColumnSchema() { }

        // 🔥 ИДЕАЛЬНЫЙ КОНСТРУКТОР: принимает структуру метаданных целиком!
        public ColumnSchema(RawColumnMetadata metadata, Func<string, ColumnType> typeParser)
        {
            Name = metadata.Name;
            OriginalName = metadata.Name;
            Type = typeParser(metadata.DataType);
            IsNullable = metadata.IsNullable;

            // Проверка первичного ключа
            IsPrimaryKey = metadata.KeyType.Equals("primary", StringComparison.OrdinalIgnoreCase) ||
                           metadata.KeyType.Equals("1") ||
                           metadata.KeyType.Equals("true");

            // Проверка автоинкремента
            IsAutoIncrement = metadata.ExtraInfo.Contains("nextval") ||
                              metadata.ExtraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public ColumnSchema CloneWithTransform(Func<string?, string?> nameTransformer)
        {
            return new ColumnSchema
            {
                OriginalName = this.OriginalName ?? this.Name,
                Name = nameTransformer(this.Name),
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
