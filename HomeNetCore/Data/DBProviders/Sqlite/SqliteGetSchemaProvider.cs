using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using HomeNetCore.Helpers;
using HomeNetCore.Helpers.Exceptions;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;


namespace HomeNetCore.Data.SqliteClasses
{
    public class SqliteGetSchemaProvider : IGetSchemaProvider
    {
         const int ColumnNameIndex = 1;
         const int ColumnTypeIndex = 2;
         const int IsNullableIndex = 3;
         const int IsPrimaryKeyIndex = 5;
        private readonly ISchemaSqlInitializer _generator;
        private readonly SqliteConnection _requiredConnection;
        private readonly ILogger _logger;

        public SqliteGetSchemaProvider(
            ISchemaSqlInitializer generator,
            SqliteConnection connection,
            ILogger logger)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым", nameof(tableName));

            try
            {
                using var command = _requiredConnection.CreateCommand();
                command.CommandText = _generator.GenerateGetTableStructureSql(tableName);

                using var reader = await command.ExecuteReaderAsync();

                var columns = await ReadColumnsAsync(reader);

                _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName}");
                

                var getSchema = new TableSchema
                {
                    TableName = tableName,
                    Columns = columns
                };

                getSchema.Initialize();

               _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {getSchema.columnNames}");

                return getSchema;
                
            }
            catch (Exception ex)
            {
                throw new SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName} в SQLite: {ex.Message}",
                    ex);
            }
        }

        private async Task<List<ColumnSchema>> ReadColumnsAsync(DbDataReader reader)
        {
            var columns = new List<ColumnSchema>();

            while (await reader.ReadAsync())
            {
                var column = CreateColumnSchema(reader);
                columns.Add(column);
            }

            return columns;
        }

        private ColumnSchema CreateColumnSchema(DbDataReader reader)
        {
            string columnName = reader.GetString(ColumnNameIndex);
            string dbType = reader.GetString(ColumnTypeIndex);
            bool isNullable = !reader.GetBoolean(IsNullableIndex);
            bool isPrimaryKey = reader.GetInt32(IsPrimaryKeyIndex) > 0;

            return new ColumnSchema
            {
                Name = columnName,
                OriginalName = columnName,
                Type = MapType(dbType),
                IsNullable = isNullable,
                IsPrimaryKey = isPrimaryKey
            };
        }



        public ColumnType MapType(string? dbType)
        {
            if (dbType is null)
            {
                return ColumnType.Unknown;
            }

            var type = dbType.ToLower();
            return type switch
            {
                "integer" => ColumnType.Integer,
                "text" => ColumnType.Varchar,
                "datetime" => ColumnType.DateTime,
                "date" => ColumnType.DateTime,
                "timestamp" => ColumnType.DateTime,
                "real" => ColumnType.DateTime,
                "boolean" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            };
        }
    }


}
