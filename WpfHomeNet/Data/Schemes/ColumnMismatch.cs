namespace WpfHomeNet.Data.Schemes
{
    public class ColumnMismatch
    {
        public string? ColumnName { get; set; }
        public ColumnSchema? Expected { get; set; }
        public ColumnSchema? Actual { get; set; }
    }
}
