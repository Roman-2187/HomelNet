using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    public class SqliteUserSqlGen : IUserSqlGenerator
    {
        private readonly TableSchema _originalTable;
        private readonly TableSchema _formattedTable;
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;

        public SqliteUserSqlGen(
            TableSchema tableSchema,
            ISchemaAdapter adapter,
            ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _originalTable = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
            _formattedTable = adapter.ConvertToSnakeCaseSchema(tableSchema) ?? throw new ArgumentNullException(nameof(adapter));

            if (string.IsNullOrEmpty(tableSchema.TableName))
            {
                _logger.LogError("Имя таблицы не может быть пустым или null");
                throw new ArgumentException("Таблица не может быть пустой");
            }
        }


        public string GenerateInsert()
        {
            if (string.IsNullOrEmpty(_formattedTable.InsertFields) || string.IsNullOrEmpty(_formattedTable.InsertParameters))
            {
                throw new InvalidOperationException("Некорректные поля или параметры для вставки");
            }
            return $@"INSERT INTO {_formattedTable.TableName} ({_formattedTable.InsertFields}) VALUES ({_formattedTable.InsertParameters});
            SELECT last_insert_rowid() AS id";
        }


        public string GenerateUpdate()
        {
            string idColumn = _formattedTable.IdColumnName
                ?? throw new InvalidOperationException("ID-колонка не найдена в таблице");

            if (string.IsNullOrEmpty(_formattedTable.SetClause))
            {
                throw new InvalidOperationException("Некорректный SET clause для обновления");
            }

            return $"UPDATE {_formattedTable.TableName} SET {_formattedTable.SetClause} WHERE {idColumn} = @{idColumn}";
        }


        public string GenerateDelete()
        {
            string idColumn = _formattedTable.IdColumnName
                ?? throw new InvalidOperationException("ID-колонка не найдена в таблице");
            return $"DELETE  FROM {_formattedTable.TableName} WHERE {idColumn} = @{idColumn}";
        }


        public string GenerateSelectById()
        {
            string idColumn = _formattedTable.IdColumnName
                ?? throw new InvalidOperationException("ID-колонка не найдена в таблице");
            return $"SELECT * FROM {_formattedTable.TableName} WHERE {idColumn} = @{idColumn}";
        }


        public string GenerateSelectByEmail()
        {
            string emailColumn = _formattedTable.Columns
                .FirstOrDefault(c => c.Name == "email")?.Name
                ?? throw new InvalidOperationException("Колонка email не найдена в таблице");

            if (!_formattedTable.Columns.Any(c => c.Name == emailColumn))
            {
                throw new InvalidOperationException($"Колонка {emailColumn} не существует в таблице");
            }

            return $"SELECT {_formattedTable.AllFields} FROM {_formattedTable.TableName} WHERE {emailColumn} = @{emailColumn}";
        }


        public string GenerateSelectAll()
        {
            if (string.IsNullOrEmpty(_formattedTable.AllFields))
            {
                throw new InvalidOperationException("Некорректные поля для выборки");
            }

            return $"SELECT {_formattedTable.AllFields} FROM {_formattedTable.TableName}";
        }


        public string GenerateEmailExists()
        {
            // Получаем имя колонки email с учетом форматирования
            string emailColumn = _formattedTable.Columns
                .FirstOrDefault(c => c.Name == "email")
                ?.Name ??
                throw new InvalidOperationException("Колонка email не найдена в таблице");

            // Формируем SQL-запрос с правильным экранированием
            return $@"
            SELECT COUNT(*) 
            FROM {_formattedTable.TableName} 
            WHERE {emailColumn} = @email";
    
        }

    }
}
