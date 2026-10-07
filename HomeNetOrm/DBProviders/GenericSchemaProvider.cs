using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Extensions;
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

        public DbProviderSpecificationExtensions Spec { get; }

        public GenericSchemaProvider(ISchemaSqlInitializer sqlInit, DbConnection connection, DbProviderSpecificationExtensions spec, ILogger logger)
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
                    // 🔥 МАГИЯ РАСШИРЕНИЙ: Читаем метаданные прямо из ридера, передавая спеку!
                    rawColumnsData.Add(reader.ReadColumnMetadata(spec));
                }
            }



            return rawColumnsData;
        }

        private TableSchema ProcessAndValidateSchema(string tableName, List<RawColumnMetadata> rawRows)
        {
            var columns = new List<ColumnSchema>();
            foreach (var row in rawRows)
            {
                // 🎯 ФИКС: Передаем правильное имя свойства ParsePropertyType из спецификации провайдера!
                columns.Add(new ColumnSchema(row, Spec.ParsePropertyType));
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

       
    }
}
