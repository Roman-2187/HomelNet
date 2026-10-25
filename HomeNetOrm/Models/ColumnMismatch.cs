namespace HomeNetOrm.Models
{
    public class ColumnMismatch
    {
        public string ColumnName { get; set; } = string.Empty;
        public required ColumnSchema Expected { get; set; } 
        public required ColumnSchema Actual { get; set; }
    }
}
