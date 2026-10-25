using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Base
{
    internal class DBInitializer
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlInitializer _schemaSqlInitializer;
        private readonly ISchemaProvider _schemaProvider;
        private readonly IEnumerable<TableSchema> _tableSchemas;
        private readonly ILogger _logger;
        private readonly string _dbName;

        public DBInitializer(
            DbConnection connection,
            ISchemaProvider schemaProvider,
            ISchemaSqlInitializer schemaSqlInitializer,
            IEnumerable<TableSchema> tableSchemas,
            ILogger logger)
        {
            _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _schemaSqlInitializer = schemaSqlInitializer ?? throw new ArgumentNullException(nameof(schemaSqlInitializer));
            _tableSchemas = tableSchemas ?? throw new ArgumentNullException(nameof(tableSchemas));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            _dbName = _dbConnection.GetType().Name.Contains("NpgsqlConnection") ? "PostgreSQL" : "SQLite";
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo($"=== 🚀 СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ [{_dbName}] ===");

            // 🌟 LINQ-фильтрация: Убираем из цепочки весь мусор, null-объекты и пустые схемы одной декларативной строкой
            var validSchemas = _tableSchemas
                .Where(s => s != null && !string.IsNullOrEmpty(s.TableName))
                .Select(s => (Raw: s, Adapted: s.ToAdaptedSchemas()))
                .Where(x => x.Adapted?.Snake?.TableName != null && x.Adapted?.Camel?.TableName != null);

            foreach (var (raw, adapted) in validSchemas)
            {
                var camelName = adapted.Camel.TableName;
                var snakeSchema = adapted.Snake;

                try
                {
                    _logger.LogInfo($"[{_dbName}] 🔍 Проверка таблицы: {camelName}...");

                    if (!await TableExistsAsync(snakeSchema.TableName))
                    {
                        await CreateTableAsync(snakeSchema, camelName);
                    }
                    else
                    {
                        _logger.LogInfo($"[{_dbName}] Таблица {camelName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(snakeSchema, camelName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{_dbName} ❌ КРИТ] Сбой при обработке таблицы {camelName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo($"=== ✨ ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ [{_dbName}] ЗАВЕРШЕНА ===");
        }

        private async Task<bool> TableExistsAsync(string dbTableName)
        {
            var cmd = _schemaSqlInitializer.GenerateTableExistsSql(dbTableName);

            try
            {
                var result = await _dbConnection.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{_dbName} ❌] Ошибка проверки существования {dbTableName}: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTableAsync(TableSchema snakeSchema, string camelTableName)
        {
            if (string.IsNullOrEmpty(snakeSchema.TableName)) return;

            _logger.LogWarning($"[{_dbName} ⚠️] Таблица {camelTableName} не обнаружена. Запуск генерации...");

            if (_dbConnection.State != ConnectionState.Open) await _dbConnection.OpenAsync();

            string createTableSql = _schemaSqlInitializer.GenerateCreateTableSql(snakeSchema);
            await _dbConnection.ExecuteAsync(createTableSql);

            if (await TableExistsAsync(snakeSchema.TableName))
                _logger.LogInfo($"[{_dbName}] ✅ Таблица {camelTableName} успешно создана в БД и готова к работе.");
            else
                _logger.LogError($"[{_dbName}] ❌ Ошибка создания! Таблица {camelTableName} отсутствует после выполнения скрипта.");
        }

        private async Task CheckTableStructureAsync(TableSchema snakeSchema, string camelTableName)
        {
            if (string.IsNullOrEmpty(snakeSchema.TableName)) return;
            if (_dbConnection.State != ConnectionState.Open) await _dbConnection.OpenAsync();

            var expectedSchemaRaw = await _schemaProvider.GetActualTableSchemaAsync(snakeSchema.TableName);
            var expectedCamelCols = expectedSchemaRaw?.ToAdaptedSchemas()?.Camel?.Columns;

            // 🌟 Guard Clause: Если схема пуста или в ней нет Primary Key — сразу выходим без глубокой вложенности
            if (expectedSchemaRaw?.Columns == null || expectedSchemaRaw.Columns.Count == 0 || !expectedSchemaRaw.Columns.Any(c => c != null && c.IsPrimaryKey))
            {
                _logger.LogError($"[{_dbName} ❌ ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{camelTableName}' пропущена, так как схема в БД повреждена или пуста.");
                return;
            }

            var diff = new SchemaComparer().Compare(expectedSchemaRaw, snakeSchema);
            if (diff == null || diff.IsIdentical)
            {
                if (diff?.IsIdentical == true) _logger.LogInfo($"[{_dbName}] ✅ Структура таблицы '{camelTableName}' в полном порядке.");
                return;
            }

            _logger.LogWarning($"[{_dbName} ⚠️] ВНИМАНИЕ: Обнаружены расхождения в структуре '{camelTableName}':");

            // Локальный хелпер, чтобы не дублировать сложный поиск имени по Camel-регистру в каждом цикле
            string GetLogName(string fallbackName) => 
                expectedCamelCols?.FirstOrDefault(c => c != null && fallbackName.Equals(c.Name, StringComparison.OrdinalIgnoreCase))?.Name ?? fallbackName;

            // 🌟 Избавляемся от ручных null-проверок внутри циклов
            if (diff.MissingColumns != null)
            {
                foreach (var missing in diff.MissingColumns.Where(m => m != null && !string.IsNullOrEmpty(m.Name)))
                {
                    _logger.LogWarning($"   [-] ❌ Отсутствует колонка: {GetLogName(missing.Name)}");
                }
            }

            if (diff.ExtraColumns != null)
            {
                foreach (var extra in diff.ExtraColumns.Where(e => e != null && !string.IsNullOrEmpty(e.Name)))
                {
                    _logger.LogWarning($"   [+] 🛑 Обнаружена лишняя колонка: {GetLogName(extra.Name)}");
                }
            }

            if (diff.MismatchedColumns != null)
            {
                var validMismatches = diff.MismatchedColumns.Where(m => m != null && !string.IsNullOrEmpty(m.ColumnName) && m.Expected != null && m.Actual != null);
                foreach (var mismatch in validMismatches)
                {
                    string reason = $"ожидалось {mismatch.Expected.Type}, прилетело {mismatch.Actual.Type}";

                    if (mismatch.Expected.IsNullable != mismatch.Actual.IsNullable)
                        reason += $" [Nullable: {mismatch.Expected.IsNullable} vs {mismatch.Actual.IsNullable}]";

                    if (mismatch.Expected.IsPrimaryKey != mismatch.Actual.IsPrimaryKey)
                        reason += $" [PK: {mismatch.Expected.IsPrimaryKey} vs {mismatch.Actual.IsPrimaryKey}]";

                    _logger.LogWarning($"[{_dbName}]    [*] 🔄 Расхождение в '{GetLogName(mismatch.ColumnName)}': {reason}");
                }
            }
        }
    }
}
