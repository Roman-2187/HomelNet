using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Extensions;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Универсальный поставщик актуальных схем данных из СУБД.
    /// Оркеструет вычитку метаданных на основе инжектируемых инициализаторов и спецификаций баз.
    /// </summary>
    public class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;
        private readonly DbConnection _requiredConnection;
        private readonly ILogger _logger;

        // Паспорт спецификации (спека)
        public DbProviderSpecificationExtensions Spec { get; }

        // В конструктор прилетает интерфейс инициализатора (Sqlite или Postgres) и нужное подключение!
        public GenericSchemaProvider(
            ISchemaSqlInitializer sqlInit,
            DbConnection connection,
            DbProviderSpecificationExtensions spec,
            ILogger logger)
        {
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlInit = sqlInit ?? throw new ArgumentNullException(nameof(sqlInit));
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
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
                // 1. Извлекаем сырые метаданные из БД через специфичный для СУБД SQL-скрипт
                var rawColumnsData = await FetchRawColumnsDataAsync(tableName);

                // 2. Трансформируем в C# модели и валидируем структуру
                return ProcessAndValidateSchema(tableName, rawColumnsData);
            }
            catch (Exception ex)
            {
                throw new SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName}: {ex.Message}", ex);
            }
        }

        private async Task<List<RawColumnMetadata>> FetchRawColumnsDataAsync(string tableName)
        {
            var rawColumnsData = new List<RawColumnMetadata>();

            using var command = _requiredConnection.CreateCommand();

            // Здесь подставится либо PRAGMA table_info для SQLite, либо SELECT из information_schema для Postgres!
            command.CommandText = _sqlInit.GenerateGetTableStructureSql(tableName);

            if (!command.Parameters.Contains("@tableName"))
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
                    // Читаем метаданные из ридера, передавая спеку конкретного диалекта
                    rawColumnsData.Add(reader.ReadColumnMetadata(Spec));
                }
            }

            return rawColumnsData;
        }

        private TableSchema ProcessAndValidateSchema(string tableName, List<RawColumnMetadata> rawRows)
        {
            var columns = new List<ColumnSchema>();
            foreach (var row in rawRows)
            {
                columns.Add(new ColumnSchema(row, Spec.ParsePropertyType));
            }

            _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName}");

            if (columns.Count == 0)
            {
                _logger.LogError($"[КРИТИЧЕСКАЯ ОШИБКА] Таблица '{tableName}' не найдена в БД или запрос метаданных вернул пустой результат!");
                return new TableSchema { TableName = tableName };
            }

            var getSchema = new TableSchema { TableName = tableName, Columns = columns };

            if (!getSchema.Initialize())
            {
                _logger.LogWarning($"[ПРЕДУПРЕЖДЕНИЕ] В таблице '{tableName}' найдено {columns.Count} колонок, но не удалось распознать Primary Key (ID). Проверьте маппинг типов!");
            }
            else
            {
                _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {getSchema.columnNames}");
            }

            return getSchema;
        }
    }
}
