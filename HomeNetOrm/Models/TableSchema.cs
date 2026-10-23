using System.Collections.Generic;
using System.Linq;

namespace HomeNetOrm.Models
{
    public class TableSchema
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new();

        // Оставляем только важный флаг логики
        public bool UseVersionTrigger { get; set; } = false;

        // Полезные легковесные хелперы для проверки ключей
        public bool HasCompositePrimaryKey => Columns.Count(c => c.IsPrimaryKey) > 1;
        public ColumnSchema? SinglePrimaryKey => Columns.Count(c => c.IsPrimaryKey) == 1 ? Columns.First(c => c.IsPrimaryKey) : null;
    }
}
