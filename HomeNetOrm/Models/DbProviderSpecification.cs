using HomeNetOrm.Enums;

namespace HomeNetOrm.Models
{
    public class DbProviderSpecification
    {
        public Func<ColumnType, bool, bool, int?, string> TypeMapper { get; }
        public Func<string, ColumnType> DbTypeParser { get; }

        public int NameIndex { get; }
        public int TypeIndex { get; }
        public int NullableIndex { get; }
        public int PrimaryKeyIndex { get; }
        public int ExtraInfoIndex { get; }

        public DbProviderSpecification(
            Func<ColumnType, bool, bool, int?, string> typeMapper,
            Func<string, ColumnType> dbTypeParser,
            int nameIndex, int typeIndex, int nullableIndex, int primarykeyIndex, int extraInfoIndex)
        {
            TypeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
            DbTypeParser = dbTypeParser ?? throw new ArgumentNullException(nameof(dbTypeParser));
            NameIndex = nameIndex;
            TypeIndex = typeIndex;
            NullableIndex = nullableIndex;
            PrimaryKeyIndex = primarykeyIndex;
            ExtraInfoIndex = extraInfoIndex;
        }
    }
}
