using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events; // 🔥 ДОБАВЛЯЕМ СЮДА: Импорт интерфейсов твоей шины событий
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DbTableInitializer
{
    public class DBInitializer
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlInitializer _schemaSqlGenerator;
        private readonly ILogger _logger;
        private readonly ISchemaProvider _schemaProvider;
        private readonly ISchemaAdapter _schemaAdapter;
        private ISchemaSqlInitializer _initializer;

        // 🔥 НАША ШИНА: Ссылка на EventBus
        private readonly IEventBus _eventBus;

        public DBInitializer(
            DbConnection connection,
            ISchemaProvider schemaProvider,
            ISchemaAdapter schemaAdapter,
            ISchemaSqlInitializer schemaSqlGenerator,
            ISchemaSqlInitializer schemaSqlInitializer,
            IEventBus eventBus, // 🔥 ВНЕДРЯЕМ: Передаем шину в конструктор
            ILogger logger)
        {
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _schemaSqlGenerator = schemaSqlGenerator ?? throw new ArgumentNullException(nameof(schemaSqlGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaAdapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
            _initializer = schemaSqlInitializer ?? throw new ArgumentNullException(nameof(schemaSqlInitializer));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus)); //
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo("=== СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ ===");

            var tableSchemas = SchemaRegistry.GetAllSchemas();

            foreach (var schema in tableSchemas)
            {
                var dbSchema = _schemaAdapter.ConvertToSnakeCaseSchema(schema);
                string dbTableName = dbSchema?.TableName ?? schema.TableName ?? string.Empty;

                try
                {
                    _logger.LogInfo($"[БД] Проверка таблицы: {dbTableName}...");

                    if (!await TableExistsAsync(dbTableName))
                    {
                        await CreateTableAsync(dbSchema ?? schema);
                    }
                    else
                    {
                        _logger.LogDebug($"[БД] Таблица {dbTableName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(schema);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[КРИТ] Сбой при обработке таблицы {dbTableName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo("=== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ЗАВЕРШЕНА ===");

            // 🔥 ВЫСТРЕЛ В АВТОБУС: Структура всех таблиц проверена и накатана!
            // Передаем в качестве отправителя 'this'. Вьюшки, просыпайтесь!
            _eventBus.Publish(this, new ISchemaSqlInitializer.DatabaseReady());
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            var cleanName = tableName.Trim('"', '\'').ToLower();
            string sql = _initializer.GenerateTableExistsSql(tableName);

            try
            {
                var result = await _dbConnection.ExecuteScalarAsync<int>(sql, new { cleanName });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка проверки существования {tableName}: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTableAsync(TableSchema dbSchema)
        {
            string targetName = dbSchema.TableName ?? string.Empty;
            _logger.LogWarning($"Таблица {targetName} не обнаружена. Запуск генерации...");

            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            await _dbConnection.ExecuteAsync(_schemaSqlGenerator.GenerateCreateTableSql(dbSchema));

            if (await TableExistsAsync(targetName))
                _logger.LogInfo($"✅ Таблица {targetName} успешно создана в БД.");
            else
            {
                _logger.LogError($"❌ Ошибка создания! Таблица {targetName} отсутствует после выполнения скрипта.");
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
                _logger.LogError($"[ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{dbTableName}'" +
                    $" пропущена, так как схема в БД повреждена, пуста или не имеет Primary Key.");
                return;
            }

            var expectedAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(expectedSchema) ??
                throw new ArgumentNullException(nameof(expectedSchema));

            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedAdaptedSchema, actualAdaptedSchema);

            if (diff.IsIdentical)
            {
                _logger.LogInfo($"  -> Структура таблицы {dbTableName} в порядке.");
            }
            else
            {
                _logger.LogWarning($"⚠️ Обнаружены расхождения в структуре {dbTableName}:");

                foreach (var missing in diff.MissingColumns)
                    _logger.LogWarning($"   [-] Отсутствует колонка: {missing.Name}");

                foreach (var extra in diff.ExtraColumns)
                    _logger.LogWarning($"   [+] Обнаружена лишняя колонка: {extra.Name}");

                foreach (var mismatch in diff.MismatchedColumns)
                    _logger.LogWarning($"   [*] Сдвиг типа в '{mismatch.ColumnName}':" +
                        $" ожидалось {mismatch.Expected}, прилетело {mismatch.Actual}");
            }
        }
    }
}
