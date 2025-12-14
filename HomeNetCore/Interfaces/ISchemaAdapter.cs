using HomeNetCore.Data.Schemes;
using WpfHomeNet.Data.Schemes;

namespace HomeNetCore.Data.Adapters
{
    public interface ISchemaAdapter
    {         
        string ConvertTableName(string rawName, NameFormat format);
         string ConvertColumnName(string? rawName, NameFormat format);
        List<string> GetColumnDefinitions(TableSchema schema);
        TableSchema? ConvertToSnakeCaseSchema(TableSchema tableSchema);
    }

  public  enum NameFormat
    {
        SnakeCase,
        CamelCase
    }

}
