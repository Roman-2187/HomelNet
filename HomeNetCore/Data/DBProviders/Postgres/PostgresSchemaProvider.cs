using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using Npgsql;
using System.Data;
using WpfHomeNet.Data.Generators.SqlQueriesGenerator;


namespace WpfHomeNet.Data.PostgreClasses
{
    public class PostgresSchemaProvider : IGetSchemaProvider
    {
        private readonly ISchemaSqlInitializer _generator;
        private readonly NpgsqlConnection _requiredConnection;

        public PostgresSchemaProvider(
            ISchemaSqlInitializer generator,
            NpgsqlConnection connection) // Явно требуем NpgsqlConnection
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (_requiredConnection.State != ConnectionState.Open)
                throw new InvalidOperationException("Соединение с PostgreSQL должно быть открыто!");
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            var columns = new List<ColumnSchema>();

            using var command = _requiredConnection.CreateCommand();
            command.CommandText = _generator.GenerateGetTableStructureSql(tableName);

            // Параметры уже строго типизированы для PostgreSQL
            command.Parameters.AddWithValue("@tableName", tableName);

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
