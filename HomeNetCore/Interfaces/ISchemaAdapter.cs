using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;


namespace HomeNetCore.Data.Interfaces
{
    public interface ISchemaAdapter
    {         
        string ConvertTableName(string rawName, NameFormat format);
         string ConvertColumnName(string? rawName, NameFormat format);
        List<string> GetColumnDefinitions(TableSchema schema);
        TableSchema? ConvertToSnakeCaseSchema(TableSchema tableSchema);



        ColumnType MapDbSpecificationType(string dbType);

        // Индексы для универсального чтения системных таблиц
        int NameIndex { get; }
        int TypeIndex { get; }
        int NullableIndex { get; }
        int PrimaryKeyIndex { get; }
        int ExtraInfoIndex { get; }
    }

  public  enum NameFormat
    {
        SnakeCase,
        CamelCase
    }

}
