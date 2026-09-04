using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetCore.Data.PostgreClasses
{
    public class PostgresSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _generator;
        private readonly DbConnection _requiredConnection;

        public PostgresSchemaProvider(ISchemaSqlInitializer generator, DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection is not NpgsqlConnection)
                throw new ArgumentException($"Поддерживаются только подключения к Postgres. Получено: {connection.GetType().Name}", nameof(connection));

            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _requiredConnection = connection;

            if (_requiredConnection.State != ConnectionState.Open)
                throw new InvalidOperationException("Соединение с PostgreSQL должно быть открыто!");
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            // 🛡️ Защита от Null: если имя таблицы не передано, падаем сразу
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            var columns = new List<ColumnSchema>();

            using var command = _requiredConnection.CreateCommand();
            command.CommandText = _generator.GenerateGetTableStructureSql(tableName);

            if (command is NpgsqlCommand npgsqlCmd)
            {
                npgsqlCmd.Parameters.Add("@tableName", NpgsqlDbType.Text).Value = tableName;
            }

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // Безопасное чтение строк с подстраховкой на случай DBNull
                string columnName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                string dbDataType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                // В Postgres IsNullable возвращает 'YES' или 'NO'
                string isNullableStr = reader.IsDBNull(3) ? "NO" : reader.GetString(3);

                // Корректно вытаскиваем информацию о ключах из ридера СУБД
                // Заметка: в Postgres структура может возвращать другие маркеры, подстрахуемся
                string keyType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                string extraInfo = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

                columns.Add(new ColumnSchema
                {
                    Name = columnName,
                    OriginalName = columnName, // Синхронизируем, чтобы маппер не потерял связь
                    Type = MapType(dbDataType),
                    Length = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    IsNullable = isNullableStr.Equals("YES", StringComparison.OrdinalIgnoreCase),
                    IsPrimaryKey = keyType.Equals("PRI", StringComparison.OrdinalIgnoreCase) || keyType.Contains("primary"),
                    IsAutoIncrement = extraInfo.Contains("nextval") || extraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase)
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
                "int4" => ColumnType.Integer,
                "smallint" => ColumnType.Integer,
                "int2" => ColumnType.Integer,
                "bigint" => ColumnType.Integer,
                "int8" => ColumnType.Integer,
                "serial" => ColumnType.Integer,
                "bigserial" => ColumnType.Integer,

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

                // Логический тип
                "boolean" => ColumnType.Boolean,
                "bool" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            };
        }
    }
}
