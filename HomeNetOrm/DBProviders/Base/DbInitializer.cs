using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DbTableInitializer
{
    /// <summary>
    /// Универсальный сверщик и создатель таблиц.
    /// Работает строго с одним инжектируемым шлейфом БД и его диалектным инициализатором.
    /// </summary>
    public class DBInitializer
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlInitializer _schemaSqlInitializer;
        private readonly ISchemaProvider _schemaProvider;
        private readonly ISchemaAdapter _schemaAdapter;
        private readonly ILogger _logger;

        // 🔥 Динамический маркер имени базы данных
        private readonly string _dbName;

        public DBInitializer(
            DbConnection connection,
            ISchemaProvider schemaProvider,
            ISchemaAdapter schemaAdapter,
            ISchemaSqlInitializer schemaSqlInitializer,
            ILogger logger)
        {
            _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _schemaAdapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
            _schemaSqlInitializer = schemaSqlInitializer ?? throw new ArgumentNullException(nameof(schemaSqlInitializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 🎯 АВТООПРЕДЕЛЕНИЕ: вычисляем имя базы данных по типу класса подключения!
            _dbName = _dbConnection.GetType().Name switch
            {
                "NpgsqlConnection" => "PostgreSQL",
                "SqliteConnection" => "SQLite",
                _ => "СУБД"
            };
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo($"=== СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ [{_dbName}] ===");

            var tableSchemas = SchemaRegistry.GetAllSchemas();

            foreach (var schema in tableSchemas)
            {
                var dbSchema = _schemaAdapter.ConvertToSnakeCaseSchema(schema);
                string dbTableName = dbSchema?.TableName ?? schema.TableName ?? string.Empty;

                try
                {
                    _logger.LogInfo($"[{_dbName}] Проверка таблицы: {dbTableName}...");

                    if (!await TableExistsAsync(dbTableName))
                    {
                        await CreateTableAsync(dbSchema ?? schema);
                    }
                    else
                    {
                        _logger.LogDebug($"[{_dbName}] Таблица {dbTableName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(schema);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{_dbName} КРИТ] Сбой при обработке таблицы {dbTableName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo($"=== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ [{_dbName}] ЗАВЕРШЕНА ===");
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            var cleanName = tableName.Trim('"', '\'').ToLower();

            string sql = _schemaSqlInitializer.GenerateTableExistsSql(tableName);

            try
            {
                var result = await _dbConnection.ExecuteScalarAsync<int>(sql, new { cleanName });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{_dbName}] Ошибка проверки существования {tableName}: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTableAsync(TableSchema dbSchema)
        {
            string targetName = dbSchema.TableName ?? string.Empty;
            _logger.LogWarning($"[{_dbName}] Таблица {targetName} не обнаружена. Запуск генерации...");

            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            string createTableSql = _schemaSqlInitializer.GenerateCreateTableSql(dbSchema);
            await _dbConnection.ExecuteAsync(createTableSql);

            if (await TableExistsAsync(targetName))
                _logger.LogInfo($"[{_dbName}] ✅ Таблица {targetName} успешно создана в БД.");
            else
            {
                _logger.LogError($"[{_dbName}] ❌ Ошибка создания! Таблица {targetName} отсутствует после выполнения скрипта.");
            }
        }

        private async Task CheckTableStructureAsync(TableSchema actualSchema)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            var actualAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(actualSchema) ??
                throw new ArgumentNullException(nameof(actualSchema));

            string dbTableName = actualAdaptedSchema.TableName ?? string.Empty;
            var expectedSchema = await _schemaProvider.GetActualTableSchemaAsync(dbTableName);

            if (expectedSchema.Columns.Count == 0 || string.IsNullOrEmpty(expectedSchema.IdColumnName))
            {
                _logger.LogError($"[{_dbName} ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{dbTableName}' пропущена, так как схема в БД повреждена, пуста или не имеет Primary Key.");
                return;
            }

            var expectedAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(expectedSchema) ??
                throw new ArgumentNullException(nameof(expectedSchema));

            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedAdaptedSchema, actualAdaptedSchema);

            if (diff.IsIdentical)
            {
                _logger.LogInfo($"  -> [{_dbName}] Структура таблицы {dbTableName} в порядке.");
            }
            else
            {
                _logger.LogWarning($"⚠️ [{_dbName}] Обнаружены расхождения в структуре {dbTableName}:");

                foreach (var missing in diff.MissingColumns)
                    _logger.LogWarning($"   [-] Отсутствует колонка: {missing.Name}");

                foreach (var extra in diff.ExtraColumns)
                    _logger.LogWarning($"   [+] Обнаружена лишняя колонка: {extra.Name}");

                foreach (var mismatch in diff.MismatchedColumns)
                    _logger.LogWarning($"   [*] Сдвиг типа в '{mismatch.ColumnName}': ожидалось {mismatch.Expected}, прилетело {mismatch.Actual}");
            }
        }
    }
}
