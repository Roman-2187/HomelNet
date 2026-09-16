namespace HomeNetOrm.Models
{
    public class SchemaDiff
    {
        public string? TableName { get; set; }
        public List<ColumnSchema> MissingColumns { get; set; } = [];
        public List<ColumnSchema> ExtraColumns { get; set; } = [];
        public List<ColumnMismatch> MismatchedColumns { get; set; } = [];

        public bool IsIdentical => !MissingColumns.Any() &&
                                  !ExtraColumns.Any() &&
                                  !MismatchedColumns.Any();
    }


}
