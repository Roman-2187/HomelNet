using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    public class PostgresUserSqlGen : ISchemaUserSqlGenerator
    {


        private readonly TableSchema _originalTable;
        private readonly TableSchema _formattedTable;
        private readonly ISchemaAdapter _adapter;
        private readonly ILogger _logger;


       
        public PostgresUserSqlGen(
            TableSchema tableSchema,
            ISchemaAdapter adapter,
            ILogger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _originalTable = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
            _formattedTable = adapter.ConvertToSnakeCaseSchema(tableSchema) ?? throw new ArgumentNullException(nameof(adapter));

            if (string.IsNullOrEmpty(_originalTable.TableName))
            {
                _logger.LogError("Имя таблицы не может быть пустым или null");
                throw new ArgumentException("Таблица не может быть пустой");
            }
        }

        public string GenerateInsert()
        {
            // В PostGreSQL можно использовать более короткий синтаксис
            return $"INSERT INTO {_originalTable.TableName} ({_originalTable.InsertFields}) VALUES ({_originalTable.InsertParameters})";
        }

        public string GenerateUpdate()
        {
            return $"UPDATE {_originalTable.TableName} SET {_originalTable.SetClause} WHERE Id = @Id";
        }

        public string GenerateDelete()
        {
            return $"DELETE FROM {_originalTable.TableName} WHERE Id = @Id";
        }

        public string GenerateSelectById()
        {
            return $"SELECT {_originalTable.AllFields} FROM {_originalTable.TableName} WHERE Id = @Id";
        }

        public string GenerateSelectByEmail()
        {
            return $"SELECT {_originalTable.AllFields} FROM {_originalTable.TableName} WHERE Email = @Email";
        }

        public string GenerateSelectAll()
        {
            return $"SELECT {_originalTable.AllFields} FROM {_originalTable.TableName}";
        }

        // Дополнительные методы для PostGreSQL
        public string GenerateUpsert()
        {
            return $@"
          INSERT INTO {_originalTable.TableName} ({_originalTable.InsertFields}) 
          VALUES ({_originalTable.InsertParameters})
          ON CONFLICT (Id) 
          DO UPDATE SET {_originalTable.SetClause}";
        }

        public string GenerateSelectCount()
        {
            return $"SELECT COUNT(*) FROM {_originalTable.TableName}";
        }


        public string GenerateEmailExists()
        {
           return string.Empty ;

        }
    }


}
