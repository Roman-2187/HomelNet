using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
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

            // Если это сервер — накатываем триггеры версионирования через методы расширения Postgres
            if (_engineType == DbEngineType.RemoteServer)
            {
                _logger.LogInfo("[SiberNet Engine] Накатываем серверные триггеры версионирования...");
                // Здесь вызываешь твой PostgresSchemaExtensions, передавая туда _schemaGenerator и _translator
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
