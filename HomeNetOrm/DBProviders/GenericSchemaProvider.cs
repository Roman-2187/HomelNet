using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders
{
    public class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;   
        private readonly DbConnection _requiredConnection;
        private readonly ILogger _logger;

        public DbProviderSpecification Spec { get; }

        public GenericSchemaProvider(ISchemaSqlInitializer sqlInit, DbConnection connection,  DbProviderSpecification spec, ILogger logger)  
                     
        {
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlInit = sqlInit ?? throw new ArgumentNullException(nameof(sqlInit));      
            Spec = spec ?? throw new ArgumentNullException(nameof(spec)); // Зафиксировали
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
                // 1. Извлекаем сырые метаданные из БД в память, используя индексы спецификации
                var rawColumnsData = await FetchRawColumnsDataAsync(tableName);

                // 2. Трансформируем в C# модели через умный конструктор ColumnSchema и валидируем
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
        private async Task<List<RawColumnMetadata>> FetchRawColumnsDataAsync(string tableName)
        {
            var rawColumnsData = new List<RawColumnMetadata>();

            using var command = _requiredConnection.CreateCommand();
            command.CommandText = _sqlInit.GenerateGetTableStructureSql(tableName);

            if (command.Parameters.Contains("@tableName") == false)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@tableName";
                param.Value = tableName;
                command.Parameters.Add(param);
            }

            var spec = Spec;

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // 🔥 СТАЛО: Создаем строгий рекорд. Читать код теперь одно удовольствие!
                    rawColumnsData.Add(new RawColumnMetadata(
                        Name: reader.IsDBNull(spec.NameIndex) ? string.Empty : reader.GetString(spec.NameIndex),
                        DataType: reader.IsDBNull(spec.TypeIndex) ? string.Empty : reader.GetString(spec.TypeIndex),
                        IsNullable: ReadNullable(reader, spec.NullableIndex),
                        KeyType: ReadStringUniversal(reader, spec.PrimaryKeyIndex),
                        ExtraInfo: spec.ExtraInfoIndex >= 0 && !reader.IsDBNull(spec.ExtraInfoIndex)
                            ? reader.GetValue(spec.ExtraInfoIndex)?.ToString() ?? string.Empty
                            : string.Empty
                    ));
                }
            }

            return rawColumnsData;
        }

        private TableSchema ProcessAndValidateSchema(string tableName, List<RawColumnMetadata> rawRows)
        {
            var columns = new List<ColumnSchema>();
            foreach (var row in rawRows)
            {
                // 🔥 ЧИСТАЯ МАГИЯ: Передаем рекорд целиком и парсер типов из спеки провайдера!
                columns.Add(new ColumnSchema(row, Spec.DbTypeParser));
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
                    $"найдено {columns.Count} колонок, но не удалось распознать Primary Key (ID). " +
                    $"Проверьте маппинг типов!");
            }
            else
            {
                _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {getSchema.columnNames}");
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
