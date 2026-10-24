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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _dbName = _dbConnection.GetType().Name.Contains("NpgsqlConnection") ? "PostgreSQL" : "SQLite";
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo($"=== 🚀 СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ [{_dbName}] ===");

            foreach (var rawSchema in _tableSchemas)
            {
                if (rawSchema == null || string.IsNullOrEmpty(rawSchema.TableName)) continue;

                var adapted = rawSchema.ToAdaptedSchemas();
                if (adapted == null || adapted.Snake == null || adapted.Camel == null) continue;

                if (string.IsNullOrEmpty(adapted.Snake.TableName) || string.IsNullOrEmpty(adapted.Camel.TableName))
                    continue;

                try
                {
                    _logger.LogInfo($"[{_dbName}] 🔍 Проверка таблицы: {adapted.Camel.TableName}...");

                    if (!await TableExistsAsync(adapted.Snake.TableName))
                    {
                        await CreateTableAsync(adapted.Snake, adapted.Camel.TableName);
                    }
                    else
                    {
                        _logger.LogInfo($"[{_dbName}] Таблица {adapted.Camel.TableName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(adapted.Snake, adapted.Camel.TableName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{_dbName} ❌ КРИТ] Сбой при обработке таблицы {adapted.Camel.TableName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo($"=== ✨ ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ [{_dbName}] ЗАВЕРШЕНА ===");
        }

        private async Task<bool> TableExistsAsync(string dbTableName)
        {
            // 🎯 ВЫЗОВ ЧЕРЕЗ ТОЧКУ: Получаем команду проверки существования
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

            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            string createTableSql = _schemaSqlInitializer.GenerateCreateTableSql(snakeSchema);
            await _dbConnection.ExecuteAsync(createTableSql);

            if (await TableExistsAsync(snakeSchema.TableName))
                _logger.LogInfo($"[{_dbName}] ✅ Таблица {camelTableName} успешно создана в БД и готова к работе.");
            else
            {
                _logger.LogError($"[{_dbName}] ❌ Ошибка создания! Таблица {camelTableName} отсутствует после выполнения скрипта.");
            }
        }

        private async Task CheckTableStructureAsync(TableSchema snakeSchema, string camelTableName)
        {
            if (string.IsNullOrEmpty(snakeSchema.TableName)) return;

            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            var expectedSchemaRaw = await _schemaProvider.GetActualTableSchemaAsync(snakeSchema.TableName);
            if (expectedSchemaRaw == null || expectedSchemaRaw.Columns == null) return;

            var adaptedExpected = expectedSchemaRaw.ToAdaptedSchemas();
            if (adaptedExpected == null) return;

            var expectedSchemaCamel = adaptedExpected.Camel;
            if (expectedSchemaCamel == null || expectedSchemaCamel.Columns == null) return;

            bool hasPrimaryKey = expectedSchemaRaw.Columns.Any(c => c != null && c.IsPrimaryKey);

            if (expectedSchemaRaw.Columns.Count == 0 || !hasPrimaryKey)
            {
                _logger.LogError($"[{_dbName} ❌ ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{camelTableName}' пропущена, так как схема в БД повреждена или пуста.");
                return;
            }

            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedSchemaRaw, snakeSchema);

            if (diff == null) return;

            if (diff.IsIdentical)
            {
                _logger.LogInfo($"[{_dbName}] ✅ Структура таблицы '{camelTableName}' в полном порядке.");
            }
            else
            {
                _logger.LogWarning($"[{_dbName} ⚠️] ВНИМАНИЕ: Обнаружены расхождения в структуре '{camelTableName}':");

                if (diff.MissingColumns != null)
                {
                    foreach (var missing in diff.MissingColumns)
                    {
                        if (missing == null || string.IsNullOrEmpty(missing.Name)) continue;

                        var camelCol = expectedSchemaCamel.Columns.FirstOrDefault(c => c != null && missing.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                        string logName = camelCol?.Name ?? missing.Name;

                        _logger.LogWarning($"   [-] ❌ Отсутствует колонка: {logName}");
                    }
                }

                if (diff.ExtraColumns != null)
                {
                    foreach (var extra in diff.ExtraColumns)
                    {
                        if (extra == null || string.IsNullOrEmpty(extra.Name)) continue;

                        var camelCol = expectedSchemaCamel.Columns.FirstOrDefault(c => c != null && extra.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                        string logName = camelCol?.Name ?? extra.Name;

                        _logger.LogWarning($"   [+] 🛑 Обнаружена лишняя колонка: {logName}");
                    }
                }

                if (diff.MismatchedColumns != null)
                {
                    foreach (var mismatch in diff.MismatchedColumns)
                    {
                        if (mismatch == null || string.IsNullOrEmpty(mismatch.ColumnName) || mismatch.Expected == null || mismatch.Actual == null)
                            continue;

                        var camelCol = expectedSchemaCamel.Columns.FirstOrDefault(c => c != null && mismatch.ColumnName.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                        string logName = camelCol?.Name ?? mismatch.ColumnName;

                        string reason = $"ожидалось {mismatch.Expected.Type}, прилетело {mismatch.Actual.Type}";

                        if (mismatch.Expected.IsNullable != mismatch.Actual.IsNullable)
                        {
                            reason += $" [Nullable: {mismatch.Expected.IsNullable} vs {mismatch.Actual.IsNullable}]";
                        }

                        if (mismatch.Expected.IsPrimaryKey != mismatch.Actual.IsPrimaryKey)
                        {
                            reason += $" [PK: {mismatch.Expected.IsPrimaryKey} vs {mismatch.Actual.IsPrimaryKey}]";
                        }

                        _logger.LogWarning($"[{_dbName}]    [*] 🔄 Расхождение в '{logName}': {reason}");
                    }
                }
            }
        }
    }
}
