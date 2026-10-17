using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.DbInfrastructure
{
   internal interface ISchemaAdapter
    {

        string ConvertTableName(string rawName, NameFormat format);
        string ConvertColumnName(string rawName, NameFormat format);
        List<string> GetColumnDefinitions(TableSchema schema);
        TableSchema? ConvertToSnakeCaseSchema(TableSchema tableSchema);

        ColumnType MapDbSpecificationType(string dbType);
    }
}
