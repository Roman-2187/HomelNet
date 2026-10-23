using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Универсальный сверщик и создатель таблиц.
    /// Работает строго с одним инжектируемым шлейфом БД и его диалектным инициализатором.
    /// </summary>
    internal class DBInitializer
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlInitializer _schemaSqlInitializer;
        private readonly ISchemaProvider _schemaProvider;
        private readonly IEnumerable<TableSchema> _tableSchemas;
        private readonly ILogger _logger;
        private readonly string _dbName;
        private readonly NameFormat _dbFormat; // 🔥 Запоминаем формат этой базы (SnakeCase или CamelCase)

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

            // Автоматически определяем имя и нужный регистр по типу соединения
            if (_dbConnection.GetType().Name.Contains("NpgsqlConnection"))
            {
                _dbName = "PostgreSQL";
                _dbFormat = NameFormat.SnakeCase;
            }
            else
            {
                _dbName = "SQLite";
                _dbFormat = NameFormat.CamelCase;
            }
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo($"=== 🚀 СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ [{_dbName}] ===");

            foreach (var schema in _tableSchemas)
            {
                if (string.IsNullOrEmpty(schema.TableName)) continue;

                // 🎯 ФИКС: Переводим имя таблицы на лету под формат текущей базы без всяких мутаций схем!
                string dbTableName = schema.TableName.ConvertTableName(_dbFormat);

                try
                {
                    _logger.LogInfo($"[{_dbName}] 🔍 Проверка таблицы: {dbTableName}...");

                    if (!await TableExistsAsync(dbTableName))
                    {
                        await CreateTableAsync(schema);
                    }
                    else
                    {
                        _logger.LogInfo($"[{_dbName}] Таблица {dbTableName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(schema, dbTableName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{_dbName} ❌ КРИТ] Сбой при обработке таблицы {dbTableName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo($"=== ✨ ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ [{_dbName}] ЗАВЕРШЕНА ===");
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
                _logger.LogError($"[{_dbName} ❌] Ошибка проверки существования {tableName}: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTableAsync(TableSchema schema)
        {
            string dbTableName = schema.TableName.ConvertTableName(_dbFormat);
            _logger.LogWarning($"[{_dbName} ⚠️] Таблица {dbTableName} не обнаружена. Запуск генерации...");

            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            string createTableSql = _schemaSqlInitializer.GenerateCreateTableSql(schema);
            await _dbConnection.ExecuteAsync(createTableSql);

            if (await TableExistsAsync(dbTableName))
                _logger.LogInfo($"[{_dbName}] ✅ Таблица {dbTableName} успешно создана в БД и готова к работе.");
            else
            {
                _logger.LogError($"[{_dbName}] ❌ Ошибка создания! Таблица {dbTableName} отсутствует после выполнения скрипта.");
            }
        }

        private async Task CheckTableStructureAsync(TableSchema actualSchema, string dbTableName)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                await _dbConnection.OpenAsync();
            }

            // Получаем то, что реально лежит в физической базе данных прямо сейчас
            var expectedSchema = await _schemaProvider.GetActualTableSchemaAsync(dbTableName);

            bool hasPrimaryKey = expectedSchema.Columns.Any(c => c.IsPrimaryKey);

            if (expectedSchema.Columns.Count == 0 || !hasPrimaryKey)
            {
                _logger.LogError($"[{_dbName} ❌ ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{dbTableName}' пропущена," +
                    $" так как схема в БД повреждена, пуста или вообще не имеет первичных ключей.");
                return;
            }

            // 🎯 СБРИЛИ ВСЕ КОПИИ ТАБЛИЦ И СТАРЫЕ КОМПАРАТОРЫ:
            // Сверяем структуры на лету с учетом регистра СУБД прямо здесь, лениво и надежно!
            var comparer = new SchemaComparer();
            var diff = comparer.Compare(expectedSchema, actualSchema, _dbFormat);

            if (diff.IsIdentical)
            {
                _logger.LogInfo($"[{_dbName}] ✅ Структура таблицы '{dbTableName}' в полном порядке.");
            }
            else
            {
                _logger.LogWarning($"[{_dbName} ⚠️] ВНИМАНИЕ: Обнаружены расхождения в структуре '{dbTableName}':");

                foreach (var missing in diff.MissingColumns)
                    _logger.LogWarning($"   [-] ❌ Отсутствует колонка: {missing.Name}");

                foreach (var extra in diff.ExtraColumns)
                    _logger.LogWarning($"   [+] 🛑 Обнаружена лишняя колонка: {extra.Name}");

                foreach (var mismatch in diff.MismatchedColumns)
                {
                    // 🔥 ФИКС 1: Добавляем восклицательный знак после Expected и Actual
                    string reason = $"ожидалось {mismatch.Expected!.Type}, прилетело {mismatch.Actual!.Type}";

                    if (mismatch.Expected!.IsNullable != mismatch.Actual!.IsNullable)
                    {
                        reason += $" [Nullable: {mismatch.Expected!.IsNullable} vs {mismatch.Actual!.IsNullable}]";
                    }

                    if (mismatch.Expected!.IsPrimaryKey != mismatch.Actual!.IsPrimaryKey)
                    {
                        reason += $" [PK: {mismatch.Expected!.IsPrimaryKey} vs {mismatch.Actual!.IsPrimaryKey}]";
                    }

                    _logger.LogWarning($"[{_dbName}]    [*] 🔄 Расхождение в '{mismatch.ColumnName}': {reason}");
                }

            }
        }
    }
}
