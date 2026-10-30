using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Generators.Sinc;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Local;
using HomeNetOrm.Repositories.Remote;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{

    public enum DbEngineType
    {
        LocalClient, // Режим клиента (SQLite)
        RemoteServer // Режим сервера (PostgreSQL)
    }
    public class SiberNetDbEngine : IDbEngine, IDisposable
    {
        private readonly DbConnection _connection;
        private readonly ILogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;
        private readonly ISqlDialectTranslator _translator;
        private readonly IDbProviderSpecification _spec;
        private readonly ISchemaSqlGenerator _schemaGenerator;
        private readonly DbEngineType _engineType;

        public DbConnection Connection => _connection;

        public SiberNetDbEngine(DbEngineType engineType, string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _engineType = engineType;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));

            // 🧙‍♂️ Настройка стратегии ОДИН РАЗ в конструкторе. Никаких рантайм if-else в фабриках!
            switch (engineType)
            {
                case DbEngineType.LocalClient:
                    _connection = new SqliteConnection(connectionString);
                    _translator = new DBProviders.Sqlite.SqliteDialectTranslator();
                    _spec = new DBProviders.Sqlite.SqliteProviderSpecification();
                    _schemaGenerator = new DBProviders.Sqlite.SqliteSchemaGenerator(_spec);
                    break;

                case DbEngineType.RemoteServer:
                    _connection = new NpgsqlConnection(connectionString);
                    _translator = new DBProviders.Postgres.PostgresDialectTranslator();
                    _spec = new DBProviders.Postgres.PostgresProviderSpecification();
                    _schemaGenerator = new DBProviders.Postgres.PostgresSchemaSqlGenerator(_spec);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(engineType), "Неизвестный тип СУБД движка.");
            }
        }

        // --- 🏗️ Абсолютно плоские и универсальные фабрики репозиториев ---

        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            // Использует наш новый универсальный SyncedUserSqlGenerator на лямбдах!
            var generator = new SyncedUserSqlGenerator(_schemas, _translator, _logger);
            return new UserRepository(_connection, generator, _logger, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            // Использует наш новый универсальный SyncedMessageSqlGenerator на лямбдах!
            var generator = new SyncedMessageSqlGenerator(_schemas, _translator, _logger);
            return new MessageRepository(_connection, generator, _logger, eventBus);
        }

        // Локальные настройки нужны только клиенту, но благодаря интерфейсу метод чист
        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            var generator = new DeviceSettingsSqlGenerator(_schemas, _translator, _logger);
            return new ClientDeviceSettingsRepository(_connection, generator);
        }

        // 🔥 ВМЕСТО ГЕНЕРАТОРА ОТДАЕМ ПУБЛИЧНО ГОТОВЫЙ РЕПОЗИТОРИЙ!
        public IDeviceSyncRepository CreateDeviceSyncRepository()
        {
            // Генератор создается здесь внутри и остается internal, инкапсуляция на все 100%!
            var generator = new DeviceSyncSqlGenerator(_schemas, _translator, _logger);

            // Возвращаем серверный репозиторий (убедись, что правильный namespace подключен)
            return new DeviceSyncRepository(_connection, generator);
        }



       
        // --- ⚙️ Универсальный запуск инициализации таблиц и схем ---
        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            // Наш универсальный сборщик провайдера схем
            var schemaProvider = new GenericSchemaProvider(_schemaGenerator, _connection, _spec, _logger);
            var tableInitializer = new SchemaRepository(_connection, schemaProvider, _schemaGenerator, _schemas, _logger);

            await tableInitializer.InitializeAsync();

            // 🔥 АВТОМАТИЧЕСКИЙ НАКАТ ТРИГГЕРОВ ДЛЯ ПОСТГРЕСА
            if (_engineType == DbEngineType.RemoteServer)
            {
                _logger.LogInfo("[SiberNet Engine] Серверный режим: Проверка системы версионирования Postgres...");

                try
                {
                    // 1. Сначала накатываем/обновляем общую функцию инкремента (там внутри OR REPLACE, так что безопасно)
                    string functionSql = _schemaGenerator.GenerateVersionFunctionSql(_translator);
                    using (var cmdFunc = _connection.CreateCommand())
                    {
                        cmdFunc.CommandText = functionSql;
                        await cmdFunc.ExecuteNonQueryAsync();
                    }
                    _logger.LogInfo("[SiberNet Engine] ✅ Глобальная функция инкремента 'update_version_func' проверена.");

                    // 📋 Черный список таблиц, которым НЕ нужно серверное версионирование и триггеры
                    var ignoreTablesForVersioning = new HashSet<string> { "device_sync", "devicesync" };

                    // 2. Бежим по всем зарегистрированным таблицам и проверяем триггеры
                    foreach (var schema in _schemas)
                    {
                        if (string.IsNullOrEmpty(schema.TableName)) continue;

                        string cleanTableName = schema.TableName.Replace("\"", "").ToLower();

                        // 🛑 Проверяем, входит ли таблица в список исключений
                        if (ignoreTablesForVersioning.Contains(cleanTableName))
                        {
                            _logger.LogInfo($"[SiberNet Engine] Игнор-лист: Таблица '{schema.TableName}' исключена из системы серверного версионирования.");
                            continue;
                        }

                        string triggerName = $"trg_{cleanTableName}_version";

                        // 🔍 Проверяем, существует ли уже этот триггер в системном каталоге pg_trigger
                        bool triggerExists = false;
                        using (var checkCmd = _connection.CreateCommand())
                        {
                            checkCmd.CommandText = "SELECT COUNT(*) FROM pg_trigger WHERE tgname = @tgname;";
                            var param = checkCmd.CreateParameter();
                            param.ParameterName = "@tgname";
                            param.Value = triggerName;
                            checkCmd.Parameters.Add(param);

                            var result = await checkCmd.ExecuteScalarAsync();
                            triggerExists = Convert.ToInt32(result) > 0;
                        }

                        if (triggerExists)
                        {
                            _logger.LogInfo($"[SiberNet Engine] Лог: Триггер версионирования '{triggerName}' уже существует для таблицы '{schema.TableName}'. Пропуск.");
                        }
                        else
                        {
                            _logger.LogWarning($"[SiberNet Engine] Лог: Триггер '{triggerName}' НЕ найден. Генерируем и накатываем...");

                            // Генерируем SQL триггера через наш метод расширения
                            string triggerSql = _schemaGenerator.GenerateVersionTriggerSql(schema, _translator);

                            using (var runCmd = _connection.CreateCommand())
                            {
                                runCmd.CommandText = triggerSql;
                                await runCmd.ExecuteNonQueryAsync();
                            }
                            _logger.LogInfo($"[SiberNet Engine] ✅ Триггер '{triggerName}' успешно привязан к таблице '{schema.TableName}'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"[SiberNet Engine] ❌ КРИТИЧЕСКАЯ ОШИБКА инициализации триггеров версионирования: {ex.Message}");
                    throw;
                }
            }
        }




        //// --- ⚙️ Универсальный запуск инициализации таблиц и схем ---
        //public async Task InitializeChannelAsync()
        //{
        //    if (_connection.State != System.Data.ConnectionState.Open)
        //    {
        //        await _connection.OpenAsync();
        //    }

        //    // Наш универсальный сборщик провайдера схем
        //    var schemaProvider = new GenericSchemaProvider(_schemaGenerator, _connection, _spec, _logger);
        //    var tableInitializer = new SchemaRepository(_connection, schemaProvider, _schemaGenerator, _schemas, _logger);

        //    await tableInitializer.InitializeAsync();

        //    // 🔥 АВТОМАТИЧЕСКИЙ НАКАТ ТРИГГЕРОВ ДЛЯ ПОСТГРЕСА
        //    if (_engineType == DbEngineType.RemoteServer)
        //    {
        //        _logger.LogInfo("[SiberNet Engine] Серверный режим: Проверка системы версионирования Postgres...");

        //        try
        //        {
        //            // 1. Сначала накатываем/обновляем общую функцию инкремента (там внутри OR REPLACE, так что безопасно)
        //            string functionSql = _schemaGenerator.GenerateVersionFunctionSql(_translator);
        //            using (var cmdFunc = _connection.CreateCommand())
        //            {
        //                cmdFunc.CommandText = functionSql;
        //                await cmdFunc.ExecuteNonQueryAsync();
        //            }
        //            _logger.LogInfo("[SiberNet Engine] ✅ Глобальная функция инкремента 'update_version_func' проверена.");

        //            // 📋 Черный список таблиц, которым НЕ нужно серверное версионирование и триггеры
        //            var ignoreTablesForVersioning = new HashSet<string> { "device_sync", "devicesync" };

        //            // 2. Бежим по всем зарегистрированным таблицам и проверяем триггеры через колонку-маркер
        //            foreach (var schema in _schemas)
        //            {
        //                if (string.IsNullOrEmpty(schema.TableName)) continue;

        //                string cleanTableName = schema.TableName.Replace("\"", "").ToLower();

        //                // 🛑 Проверяем, входит ли таблица в список исключений
        //                if (ignoreTablesForVersioning.Contains(cleanTableName))
        //                {
        //                    _logger.LogInfo($"[SiberNet Engine] Игнор-лист: Таблица '{schema.TableName}' исключена из системы серверного версионирования.");
        //                    continue;
        //                }

        //                // 🔍 КОПЕЕЧНАЯ ОПЕРАЦИЯ: Проверяем наличие колонки-маркера в таблице
        //                bool hasMarkerColumn = false;
        //                using (var checkCmd = _connection.CreateCommand())
        //                {
        //                    checkCmd.CommandText = @"
        //                        SELECT COUNT(*) 
        //                        FROM information_schema.columns 
        //                        WHERE table_name = @tableName AND column_name = 'is_triggered';";

        //                    var param = checkCmd.CreateParameter();
        //                    param.ParameterName = "@tableName";
        //                    param.Value = cleanTableName;
        //                    checkCmd.Parameters.Add(param);

        //                    var result = await checkCmd.ExecuteScalarAsync();
        //                    hasMarkerColumn = Convert.ToInt32(result) > 0;
        //                }

        //                if (hasMarkerColumn)
        //                {
        //                    _logger.LogInfo($"[SiberNet Engine] Лог: Маркер 'is_triggered' найден для таблицы '{schema.TableName}'. Триггер активен. Пропуск.");
        //                }
        //                else
        //                {
        //                    string triggerName = $"trg_{cleanTableName}_version";
        //                    _logger.LogWarning($"[SiberNet Engine] Лог: Маркер для таблицы '{schema.TableName}' НЕ найден. Генерируем триггер...");

        //                    // Атомарно накатываем триггер и добавляем колонку-маркер
        //                    // Используем транзакцию, чтобы маркер и триггер создавались строго вместе
        //                    using (var transaction = _connection.BeginTransaction())
        //                    {
        //                        try
        //                        {
        //                            // А. Создаем триггер
        //                            string triggerSql = _schemaGenerator.GenerateVersionTriggerSql(schema, _translator);
        //                            using (var runCmd = _connection.CreateCommand())
        //                            {
        //                                runCmd.Transaction = transaction;
        //                                runCmd.CommandText = triggerSql;
        //                                await runCmd.ExecuteNonQueryAsync();
        //                            }

        //                            // Б. Добавляем колонку-маркер с дефолтным значением
        //                            using (var alterCmd = _connection.CreateCommand())
        //                            {
        //                                alterCmd.Transaction = transaction;
        //                                alterCmd.CommandText = $"ALTER TABLE {schema.TableName} ADD COLUMN is_triggered BOOLEAN DEFAULT TRUE;";
        //                                await alterCmd.ExecuteNonQueryAsync();
        //                            }

        //                            await transaction.CommitAsync();
        //                            _logger.LogInfo($"[SiberNet Engine] ✅ Триггер '{triggerName}' успешно привязан, таблица '{schema.TableName}' помечена.");
        //                        }
        //                        catch
        //                        {
        //                            await transaction.RollbackAsync();
        //                            throw;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogCritical($"[SiberNet Engine] ❌ КРИТИЧЕСКАЯ ОШИБКА инициализации триггеров версионирования: {ex.Message}");
        //            throw;
        //        }
        //    }
        //}




        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
