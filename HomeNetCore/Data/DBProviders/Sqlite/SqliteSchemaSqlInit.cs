using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Builders;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;


namespace HomeNetCore.Data.DBProviders.Sqlite
{

    public class SqliteSchemaSqlInit : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private ILogger _logger;

        public SqliteSchemaSqlInit(ILogger logger,ISchemaAdapter schemaAdapter)
        {
            _adapter = schemaAdapter;
            _logger = logger;
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null)
            {
                _logger.LogError("Схема таблицы не может быть null");
                throw new ArgumentNullException(nameof(schema));
            }

            if (string.IsNullOrEmpty(schema.TableName))
            {
                _logger.LogError("Имя таблицы не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым");
            }          
            string tableName = _adapter.ConvertTableName(schema.TableName, NameFormat.SnakeCase);
           
            // Получаем определения всех колонок
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            // Формируем итоговый SQL-запрос
            string sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (" +
                   string.Join(", ", columnDefinitions) +
                   ")";

            return sql;
        }

        public string GenerateTableExistsSql(string? tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                _logger.LogError("Имя таблицы не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null");
            }


            string escapedName = _adapter.ConvertTableName(tableName,NameFormat.SnakeCase);
            string sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";           
            return sql;
        }

        public string GenerateGetTableStructureSql(string? tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                _logger.LogError("Имя таблицы не может быть пустым");
                throw new ArgumentException("Имя таблицы не может быть пустым или null");
            }
            // Используем экранированное имя таблицы
            string escapedName = _adapter.ConvertTableName(tableName,NameFormat.SnakeCase);

            return $"PRAGMA table_info(\"{escapedName}\")";
        }
      
    }

}
