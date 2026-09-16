using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;
using HomeNetCore.Exeptions;
namespace HomeNetOrm.DBProviders
{
    public class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;
        private readonly ISchemaAdapter _adapter;
        private readonly DbConnection _requiredConnection;
        private readonly ILogger _logger;

        public GenericSchemaProvider(
            ISchemaSqlInitializer sqlInit,
            ISchemaAdapter adapter,
            DbConnection connection,
            ILogger logger)
        {
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlInit = sqlInit ?? throw new ArgumentNullException(nameof(sqlInit));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableSchema> GetActualTableSchemaAsync(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым", nameof(tableName));

            if (_requiredConnection.State != ConnectionState.Open)
            {
                await _requiredConnection.OpenAsync();
            }

            try
            {
                // 1. Извлекаем сырые метаданные из БД в память
                var rawColumnsData = await FetchRawColumnsDataAsync(tableName);

                // 2. Маппим сырые данные в C# модели и валидируем результат
                return ProcessAndValidateSchema(tableName, rawColumnsData);
            }
            catch (Exception ex)
            {
                throw new SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Чтение сырых метаданных из базы данных в изоляции.
        /// </summary>
        private async Task<List<(string Name, string DataType, bool IsNullable, string KeyType, string ExtraInfo)>> FetchRawColumnsDataAsync(string tableName)
        {
            var rawColumnsData = new List<(string Name, string DataType, bool IsNullable, string KeyType, string ExtraInfo)>();

            using var command = _requiredConnection.CreateCommand();
            command.CommandText = _sqlInit.GenerateGetTableStructureSql(tableName);

            if (command.Parameters.Contains("@tableName") == false)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@tableName";
                param.Value = tableName;
                command.Parameters.Add(param);
            }

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rawColumnsData.Add((
                        Name: reader.IsDBNull(_adapter.NameIndex) ? string.Empty : reader.GetString(_adapter.NameIndex),
                        DataType: reader.IsDBNull(_adapter.TypeIndex) ? string.Empty : reader.GetString(_adapter.TypeIndex),
                        IsNullable: ReadNullable(reader, _adapter.NullableIndex),
                        KeyType: ReadStringUniversal(reader, _adapter.PrimaryKeyIndex),
                        ExtraInfo: _adapter.ExtraInfoIndex >= 0 && !reader.IsDBNull(_adapter.ExtraInfoIndex)
                            ? reader.GetValue(_adapter.ExtraInfoIndex)?.ToString() ?? string.Empty
                            : string.Empty
                    ));
                }
            }

            return rawColumnsData;
        }

        /// <summary>
        /// Трансформация сырых строк в сущности ColumnSchema и валидация TableSchema.
        /// </summary>
        private TableSchema ProcessAndValidateSchema(string tableName, List<(string Name, string DataType, bool IsNullable, string KeyType, string ExtraInfo)> rawRows)
        {
            var columns = new List<ColumnSchema>();
            foreach (var row in rawRows)
            {
                bool isPk = row.KeyType.Equals("primary", StringComparison.OrdinalIgnoreCase) ||
                            row.KeyType.Equals("1") || row.KeyType.Equals("true");

              //  isPk = false;

                columns.Add(new ColumnSchema
                {
                    Name = row.Name,
                    OriginalName = row.Name,
                    Type = _adapter.MapDbSpecificationType(row.DataType),
                    IsNullable = row.IsNullable,
                    IsPrimaryKey = isPk,
                    IsAutoIncrement = row.ExtraInfo.Contains("nextval") ||
                    row.ExtraInfo.Equals("auto_increment", StringComparison.OrdinalIgnoreCase)
                });
            }

            _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName}");

            if (columns.Count == 0)
            {
                _logger.LogError($"[КРИТИЧЕСКАЯ ОШИБКА] Таблица '{tableName}' " +
                    $"не найдена в БД или запрос метаданных вернул пустой результат!");
                return new TableSchema { TableName = tableName };
            }

            var getSchema = new TableSchema { TableName = tableName, Columns = columns };

            if (!getSchema.Initialize())
            {
                _logger.LogWarning($"[ПРЕДУПРЕЖДЕНИЕ] В таблице '{tableName}' " +
                    $"найдено {columns.Count} колонок," +
                    $" но не удалось распознать Primary Key (ID)." +
                    $" Проверьте маппинг типов!");
            }
            else
            {
                _logger.LogDebug($"Получено имен колонок таблицы" +
                    $" {tableName} : {getSchema.columnNames}");
            }

            return getSchema;
        }

        private bool ReadNullable(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return false;

            string val = reader.GetValue(index)?.ToString() ?? string.Empty;

            if (val.Equals("YES", StringComparison.OrdinalIgnoreCase)) return true;
            if (val.Equals("NO", StringComparison.OrdinalIgnoreCase)) return false;

            if (bool.TryParse(val, out bool res)) return !res;

            return val.Equals("0");
        }

        private string ReadStringUniversal(DbDataReader reader, int index)
        {
            if (index < 0 || reader.IsDBNull(index)) return string.Empty;
            return reader.GetValue(index).ToString() ?? string.Empty;
        }
    }
}

