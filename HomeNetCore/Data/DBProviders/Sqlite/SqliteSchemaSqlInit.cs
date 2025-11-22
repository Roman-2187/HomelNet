using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;
using WpfHomeNet.Data.DBProviders.SqliteClasses;

namespace WpfHomeNet.Data.SqliteClasses
{

    public class SqliteSchemaSqlInit : ISchemaSqlInitializer
    {
        private readonly SqliteSchemaAdapter _adapter;
        private ILogger _logger;

        public SqliteSchemaSqlInit(ILogger logger)
        {
            _adapter = new SqliteSchemaAdapter();
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


            // Получаем имя таблицы в формате snake_case
            string tableName = _adapter.GetTableName(schema.TableName);

           
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


            string escapedName = _adapter.GetTableName(tableName);
            string sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            _logger.LogInformation("получена строка запроса",sql);
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
            string escapedName = _adapter.GetTableName(tableName);

            return $"PRAGMA table_info(\"{escapedName}\")";
        }




        public ColumnType MapDatabaseType(string? dbType)
        {
            if (dbType is null)
            {
                _logger.LogWarning("Получено null значение для типа колонки");
                return ColumnType.Unknown;
            }

            var type = dbType.ToLower();
            switch (type)
            {
                case "integer":
                    return ColumnType.Integer;
                case "text":
                    return ColumnType.Varchar;
                case "datetime":
                    return ColumnType.DateTime;
                case "boolean":
                    return ColumnType.Boolean;
                default:
                    _logger.LogWarning($"Неизвестный тип колонки: {type}");
                    return ColumnType.Unknown;
            }
        }


    }

}
