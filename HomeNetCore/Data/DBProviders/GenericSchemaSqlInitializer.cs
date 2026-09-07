using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Data.DBProviders
{
    public class GenericSchemaSqlInitializer : ISchemaSqlInitializer
    {
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        // 🌌 НАШИ ПЕРЕМЕННЫЕ-ШАБЛОНЫ: Задаются один раз при сборке машины!
        private readonly string _tableExistsSqlTemplate;
        private readonly string _getTableStructureSql;

        public GenericSchemaSqlInitializer(
            ILogger logger,
            ISchemaAdapter schemaAdapter,
            string tableExistsSqlTemplate,
            string getTableStructureSql)
        {
            _adapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableExistsSqlTemplate = tableExistsSqlTemplate ?? throw new ArgumentNullException(nameof(tableExistsSqlTemplate));
            _getTableStructureSql = getTableStructureSql ?? throw new ArgumentNullException(nameof(getTableStructureSql));
        }

        // Этот метод ОДИНАКОВЫЙ для обеих СУБД, так как вся разница сидит внутри _adapter! ⚙️
        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null)
            {
                _logger.LogError("Схема таблицы при генерации CREATE TABLE не может быть null");
                throw new ArgumentNullException(nameof(schema));
            }

            string tableName = schema.TableName ?? throw new InvalidOperationException("Имя таблицы отсутствует в схеме.");
            List<string> columnDefinitions = _adapter.GetColumnDefinitions(schema);

            return $@"CREATE TABLE IF NOT EXISTS ""{tableName}"" ({string.Join(", ", columnDefinitions)});";
        }

        public string GenerateTableExistsSql(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым или null", nameof(tableName));

            string escapedName = _adapter.ConvertTableName(tableName, NameFormat.SnakeCase);


            // Просто подставляем имя в шаблон, переданный под конкретную СУБД! 🎯
            return string.Format(_tableExistsSqlTemplate, escapedName);
        }

        public string GenerateGetTableStructureSql(string? tableName)
{
    if (string.IsNullOrEmpty(tableName)) return _getTableStructureSql;

    // 🔥 ИСПРАВЛЕНИЕ: Если это SQLite Pragma, подставляем имя таблицы
    if (_getTableStructureSql.Contains("PRAGMA", StringComparison.OrdinalIgnoreCase))
    {
        // 1. Если шаблон использует формат string.Format (как table_info("{0}"))
        if (_getTableStructureSql.Contains("{0}"))
        {
            return string.Format(_getTableStructureSql, tableName);
        }

        // 2. Если шаблон использует старый плейсхолдер @tableName
        return _getTableStructureSql.Replace("@tableName", $"\"{tableName}\"", StringComparison.OrdinalIgnoreCase);
    }

    // Для Postgres возвращаем как есть — там отработает штатный параметр @tableName
    return _getTableStructureSql;
}


    }
}

