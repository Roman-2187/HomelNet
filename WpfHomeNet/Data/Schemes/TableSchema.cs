namespace WpfHomeNet.Data.Schemes
{
    public class TableSchema
    {
        public string? TableName { get; set; }
        public List<ColumnSchema> Columns { get; set; } = new();

       
    }
}
