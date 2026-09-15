using HomeNetOrm.Data.Schemes.CreateSchemaBd;

namespace HomeNetOrm.Data.Schemes.CheckTableBd
{
    public class ColumnMismatch
    {
        public string? ColumnName { get; set; }
        public ColumnSchema? Expected { get; set; }
        public ColumnSchema? Actual { get; set; }
    }
}
