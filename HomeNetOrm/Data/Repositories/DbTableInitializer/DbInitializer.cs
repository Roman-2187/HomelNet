using Dapper;
using HomeNetCore.Interfaces;
using HomeNetOrm.Data.Schemes.CheckTableBd;
using HomeNetOrm.Data.Schemes.CreateSchemaBd;
using HomeNetOrm.Interfaces;
using System.Data;
using System.Data.Common;


namespace HomeNetOrm.Data.Builders
{
    public class DBInitializer
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlInitializer _schemaSqlGenerator;
        private readonly ILogger _logger;
        private readonly ISchemaProvider _schemaProvider;
        private readonly ISchemaAdapter _schemaAdapter;
        private ISchemaSqlInitializer _initializer;

        public DBInitializer(
            DbConnection connection,
            ISchemaProvider schemaProvider,
            ISchemaAdapter schemaAdapter,
            ISchemaSqlInitializer schemaSqlGenerator,
            ISchemaSqlInitializer schemaSqlInitializer,
            ILogger logger)
        {
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _schemaSqlGenerator = schemaSqlGenerator ?? throw new ArgumentNullException(nameof(schemaSqlGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaAdapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
            _initializer = schemaSqlInitializer ?? throw new ArgumentNullException(nameof(schemaSqlInitializer));
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("=== СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ ===");

            var tableSchemas = SchemaRegistry.GetAllSchemas();

            foreach (var schema in tableSchemas)
            {
                // 🛡️ Переводим схему в змейку (из "Users" в "users"), чтобы узнать её РЕАЛЬНОЕ имя в БД
                var dbSchema = _schemaAdapter.ConvertToSnakeCaseSchema(schema);
                string dbTableName = dbSchema?.TableName ?? schema.TableName ?? string.Empty;

                try
                {
                    _logger.LogInformation($"[БД] Проверка таблицы: {dbTableName}...");

                    // Ищем в БД именно физическое имя "users", а не C#-имя "Users"
                    if (!await TableExistsAsync(dbTableName))
                    {
                        // Передаем на создание уже готовую snake_case схему
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

            _logger.LogInformation("=== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ЗАВЕРШЕНА ===");
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            // 🛡️ Полностью очищаем имя от кавычек и переводим в нижний регистр для сверки
            var cleanName = tableName.Trim('"', '\'').ToLower();

            // Переводим системное имя из sqlite_master в нижний регистр через LOWER() 
            // Это найдет и "Users", и "users", и "USERS" со 100% гарантией!
            // СТАЛО (идеально под любую СУБД):
            string sql = _initializer.GenerateTableExistsSql(tableName);


            try
            {
                // Передаем чистый параметр напрямую в Dapper, минуя кривой генератор
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

            // Проверяем по РЕАЛЬНОМУ имени, которое улетело в базу данных
            if (await TableExistsAsync(targetName))
                _logger.LogInformation($"✅ Таблица {targetName} успешно создана в БД.");
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

            // Переводим исходную схему в змейку для корректного поиска структуры
            var actualAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(actualSchema) ??
                throw new ArgumentNullException(nameof(actualSchema));

            string dbTableName = actualAdaptedSchema.TableName ?? string.Empty;

            // Запрашиваем состояние из базы по её правильному имени в нижнем регистре
            var expectedSchema = await _schemaProvider.GetActualTableSchemaAsync(dbTableName);

            // 🔥 ИСПРАВЛЕННЫЙ СТРАЖ: Проверяем именно то, что прилетело ИЗ БАЗЫ (expectedSchema)!
            if (expectedSchema.Columns.Count == 0 || string.IsNullOrEmpty(expectedSchema.IdColumnName))
            {
                _logger.LogError($"[ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{dbTableName}'" +
                    $" пропущена, так как схема в БД повреждена, пуста или не имеет Primary Key.");
                return; // Мгновенный выход, к сравнению ниже не идем 🛑
            }


            var expectedAdaptedSchema = _schemaAdapter.ConvertToSnakeCaseSchema(expectedSchema) ??
                throw new ArgumentNullException(nameof(expectedSchema));

            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedAdaptedSchema, actualAdaptedSchema);

            if (diff.IsIdentical)
            {
                _logger.LogInformation($"  -> Структура таблицы {dbTableName} в порядке.");
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

