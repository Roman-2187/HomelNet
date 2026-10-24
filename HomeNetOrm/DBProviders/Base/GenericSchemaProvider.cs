using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Универсальный поставщик актуальных схем данных из СУБД.
    /// Оркеструет вычитку метаданных на основе чистых SQL-запросов и Dapper.
    /// </summary>
    internal class GenericSchemaProvider : ISchemaProvider
    {
        private readonly ISchemaSqlInitializer _sqlInit;
        private readonly DbConnection _requiredConnection;
        private readonly IDbProviderSpecification _spec;
        private readonly ILogger _logger;

        public GenericSchemaProvider(
            ISchemaSqlInitializer sqlInit,
            DbConnection connection,
            IDbProviderSpecification spec,
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
                // 🎯 ЧИСТЫЙ ВЫЗОВ ЧЕРЕЗ ТОЧКУ: Получаем готовый объект команды из инициализатора
                var cmd = _sqlInit.GenerateGetTableStructureSql(tableName);

                // Передаем в Dapper скомпилированную строку и параметры прямо из команды
                var rawColumnsData = await _requiredConnection.QueryAsync<RawColumnMetadata>(cmd.Sql, cmd.Params);

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
            // 🔥 ФИКС: Используем наш чистый кастомный конструктор!
            // Он сам забирает типы, Nullable и PK из метаданных на лету, убирая кашу
            var columns = rawRows
                .Select(row => new ColumnSchema(row, dbType => _spec.ParsePropertyType(dbType)))
                .ToList();

            _logger.LogDebug($"Получено {columns.Count} столбцов для таблицы {tableName}");

            if (columns.Count == 0)
            {
                _logger.LogError($"[КРИТИЧЕСКАЯ ОШИБКА] Таблица '{tableName}' не найдена в БД или запрос метаданных вернул пустой результат!");
                return new TableSchema { TableName = tableName };
            }

            // Нам больше не нужно запускать .Initialize(), так как схема иммутабельна
            var getSchema = new TableSchema { TableName = tableName, Columns = columns };

            // Вместо чтения удалённого свойства .ColumnNames склеиваем имена для лога прямо на месте
            if (!columns.Any(c => c.IsPrimaryKey))
            {
                _logger.LogWarning($"[ПРЕДУПРЕЖДЕНИЕ] В таблице '{tableName}' найдено {columns.Count} колонок, но не удалось распознать Primary Key (ID). Проверьте маппинг типов!");
            }
            else
            {
                string columnNamesLog = string.Join(", ", columns.Select(c => c.Name));
                _logger.LogDebug($"Получено имен колонок таблицы {tableName} : {columnNamesLog}");
            }

            return getSchema;
        }
    }
}
