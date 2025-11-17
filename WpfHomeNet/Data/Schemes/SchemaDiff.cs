using WpfHomeNet.Data.Bilders;

namespace WpfHomeNet.Data.Schemes
{
    public class SchemaDiff
    {
        public string? TableName { get; set; }
        public List<ColumnSchema> MissingColumns { get; set; } = new();
        public List<ColumnSchema> ExtraColumns { get; set; } = new();
        public List<ColumnMismatch> MismatchedColumns { get; set; } = new();

        public bool IsIdentical => !MissingColumns.Any() &&
                                  !ExtraColumns.Any() &&
                                  !MismatchedColumns.Any();
    }


}
