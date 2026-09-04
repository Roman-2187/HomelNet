using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers.Exceptions; // Для красивых кастомных исключений ядра
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
        private readonly ILogger _logger; // Подключаем наш логгер 🚀

        public PostgresSchemaProvider(
            ISchemaSqlInitializer generator,
            DbConnection connection,
            ILogger logger) // Принимаем логгер через DI
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection is not NpgsqlConnection)
                throw new ArgumentException($"Поддерживаются только подключения к Postgres. Получено: {connection.GetType().Name}", nameof(connection));

            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _requiredConnection = connection;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            //if (_requiredConnection.State != ConnectionState.Open)
            //    throw new InvalidOperationException("Соединение с PostgreSQL должно быть открыто!");
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            // Списков для временного хранения сырых данных из БД до закрытия ридера
            var rawColumnsData = new List<(string Name, string DataType, int? Length, bool IsNullable, string KeyType, string ExtraInfo)>();

            try
            {
                using (var command = _requiredConnection.CreateCommand())
                {
                    command.CommandText = _generator.GenerateGetTableStructureSql(tableName);

                    if (command is NpgsqlCommand npgsqlCmd)
                    {
                        npgsqlCmd.Parameters.Add("@tableName", NpgsqlTypes.NpgsqlDbType.Text).Value = tableName;
                    }

                    // БЫСТРЫЙ ЧИТАТЕЛЬ: Только забираем данные, никакого лишнего кода внутри!
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            rawColumnsData.Add((
                                Name: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                DataType: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Length: reader.IsDBNull(2) ? null : reader.GetInt32(2),
                                IsNullable: reader.IsDBNull(3) ? false : reader.GetString(3).Equals("YES", StringComparison.OrdinalIgnoreCase),
                                KeyType: reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                ExtraInfo: reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                            ));
                        }
                    } // 🔐 Ридер гарантированно ЗАКРЫЛСЯ здесь. Подключение полностью свободно!
                }

                // Теперь спокойно, в свободной оперативной памяти, собираем наши умные модели
                var columns = new List<ColumnSchema>();
                foreach (var row in rawColumnsData)
                {
                    columns.Add(new ColumnSchema
                    {
                        Name = row.Name,
                        OriginalName = row.Name,
                        Type = MapType(row.DataType),
                        Length = row.Length,
                        IsNullable = row.IsNullable,
                        IsPrimaryKey = row.KeyType.Equals("primary", StringComparison.OrdinalIgnoreCase) || row.KeyType.Contains("primary"),
                        IsAutoIncrement = row.ExtraInfo.Contains("nextval") || row.ExtraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase)
                    });
                }

                _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName} из PostgreSQL");

                var getSchema = new TableSchema
                {
                    TableName = tableName,
                    Columns = columns
                };

                // Запускаем расчет SQL-костей Dapper на свободном подключении
                getSchema.Initialize();

                _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {getSchema.columnNames}");

                return getSchema;
            }
            catch (Exception ex)
            {
                throw new HomeNetCore.Helpers.Exceptions.SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName} в PostgreSQL: {ex.Message}",
                    ex);
            }
        }


        public ColumnType MapType(string? dbType)
        {
            if (dbType is null) return ColumnType.Unknown;

            var type = dbType.ToLower();
            return type switch
            {
                "integer" or "int4" or "smallint" or "int2" or "bigint" or "int8" or "serial" or "bigserial" => ColumnType.Integer,
                "varchar" or "character varying" or "text" or "char" or "character" => ColumnType.Varchar,
                "timestamp" or "timestamp with time zone" or "timestamp without time zone" or "date" or "time" => ColumnType.DateTime,
                "boolean" or "bool" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            };
        }
    }
}
