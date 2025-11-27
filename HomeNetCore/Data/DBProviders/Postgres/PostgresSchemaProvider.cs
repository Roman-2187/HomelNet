using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using Microsoft.Data.Sqlite;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;



namespace HomeNetCore.Data.PostgreClasses
{
    public class PostgresSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _generator;
        private readonly DbConnection _requiredConnection;

        public PostgresSchemaProvider( ISchemaSqlInitializer generator,DbConnection connection)            
        {
            // Сначала проверяем на null
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Затем проверяем тип
            if (connection is not NpgsqlConnection)throw new ArgumentException(                
                $"Only Postgres connections are supported. Received: {connection.GetType().Name}",  nameof(connection));
                  

            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _requiredConnection = connection; 

            if (_requiredConnection.State != ConnectionState.Open)
                throw new InvalidOperationException("Соединение с PostgreSQL должно быть открыто!");
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            var columns = new List<ColumnSchema>();

            using var command = _requiredConnection.CreateCommand();
            command.CommandText = _generator.GenerateGetTableStructureSql(tableName);

           
            var npgsqlCmd = (NpgsqlCommand)command;
            npgsqlCmd.Parameters.Add("@tableName", NpgsqlDbType.Text).Value =
                tableName ?? (object)DBNull.Value;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnSchema
                {
                    Name = reader.GetString(0),
                    Type = MapType(reader.GetString(1)),
                    Length = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    IsNullable = reader.GetString(3) == "YES",
                    IsPrimaryKey = reader.GetString(4) == "PRI",
                    IsAutoIncrement = reader.GetString(5) == "auto_increment"
                });
            }

            return new TableSchema
            {
                TableName = tableName,
                Columns = columns
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
                // Числовые типы
                "integer" => ColumnType.Integer,
                "smallint" => ColumnType.Integer,
                "bigint" => ColumnType.Integer,
                "serial" => ColumnType.Integer,                           
                // Строковые типы

                "varchar" => ColumnType.Varchar,
                "character varying" => ColumnType.Varchar,
                "text" => ColumnType.Varchar,
                "char" => ColumnType.Varchar,
                "character" => ColumnType.Varchar,

                // Дата и время
                "timestamp" => ColumnType.DateTime,
                "timestamp with time zone" => ColumnType.DateTime,
                "timestamp without time zone" => ColumnType.DateTime,
                "date" => ColumnType.DateTime,
                "time" => ColumnType.DateTime,
                "time with time zone" => ColumnType.DateTime,
                "time without time zone" => ColumnType.DateTime,

                // Логический тип
                "boolean" => ColumnType.Boolean,
                "bool" => ColumnType.Boolean,                            
                _ => ColumnType.Unknown
            };
        }

    }




}
