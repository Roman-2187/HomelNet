using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Универсальный поставщик актуальных схем данных из СУБД.
    /// Оркеструет вычитку метаданных на основе чистых SQL-запросов и Dapper.
    /// </summary>
    public class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;
        private readonly DbConnection _requiredConnection;
        private readonly IDbProviderSpecification _spec; // Наш новый изолированный контракт
        private readonly ILogger _logger;

        public GenericSchemaProvider(
            ISchemaSqlInitializer sqlInit,
            DbConnection connection,
            IDbProviderSpecification spec, // Инжектируем новый чистый интерфейс
            ILogger logger)
        {
            _requiredConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlInit = sqlInit ?? throw new ArgumentNullException(nameof(sqlInit));
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
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
                // 1. Быстро забираем слепок метаданных из системных таблиц одной строчкой через Dapper!
                string sql = _sqlInit.GenerateGetTableStructureSql(tableName);
                var rawColumnsData = await _requiredConnection.QueryAsync<RawColumnMetadata>(sql, new { TableName = tableName });

                // 2. Трансформируем в C# модели и валидируем структуру
                return ProcessAndValidateSchema(tableName, rawColumnsData);
            }
            catch (Exception ex)
            {
                throw new SchemaProviderException(
                    $"Ошибка при получении схемы для таблицы {tableName}: {ex.Message}", ex);
            }
        }

        private TableSchema ProcessAndValidateSchema(string tableName, IEnumerable<RawColumnMetadata> rawRows)
        {
            var columns = new List<ColumnSchema>();

            foreach (var row in rawRows)
            {
                // Собираем ColumnSchema
                columns.Add(new ColumnSchema
                {
                    Name = row.Name,
                    OriginalName = row.Name,
                    Type = _spec.ParsePropertyType(row.DataType),

                    // ⚡ ПРОСТО ПОДСТАВЛЯЕМ: Раз row.IsNullable уже булево, никакой string.Equals не нужен!
                    IsNullable = row.IsNullable,

                    // Тут проверяем, как у тебя объявлен KeyType в рекорде (если строка, оставляем так)
                    IsPrimaryKey = string.Equals(row.KeyType, "YES", StringComparison.OrdinalIgnoreCase) || row.KeyType == "1"
                });
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
