using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite; // 🆕 Подключаем транслятор
using HomeNetOrm.DBProviders.Sqlite.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.DBProviders.Sqlite.SincedGenerators;
using HomeNetOrm.DBProviders.Sqlite.SyncedGenerators;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure; // 🆕 Подключаем интерфейс транслятора
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Local;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class SincedSqliteDbEngine : IDbEngine
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ILogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;

        // 🧙‍♂️ Единый источник правды для диалекта SQLite во всем локальном движке
        private readonly ISqlDialectTranslator _translator;

        public DbConnection Connection => _connection;

        public SincedSqliteDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            // 🎯 Инициализируем транслятор под SQLite диалект при старте
            _translator = new SqliteDialectTranslator();
        }

        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            // 🔥 Передаем _translator в генератор юзеров
            var generator = new SyncedSqliteUserSqlGenerator(_schemas, _translator, _logger);
            return new UserRepository(_connection, generator, _logger, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            // 🔥 Передаем _translator в генератор сообщений
            var generator = new SyncedSqliteMessageSqlGenerator(_schemas, _translator, _logger);
            return new MessageRepository(_connection, generator, _logger, eventBus);
        }

        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            // 🔥 Передаем _translator в генератор настроек
            var generator = new SqliteDeviceSettingsSqlGenerator(_schemas, _translator, _logger);
            return new ClientDeviceSettingsRepository(_connection, generator);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var sqliteSpec = new SqliteProviderSpecification();

            // 🔥 Передаем наш _translator в генератор схем для правильного экранирования [] таблиц
            var sqliteSchemaInit = new SqliteSchemaGenerator(sqliteSpec, _translator);
            var sqliteProvider = new GenericSchemaProvider(sqliteSchemaInit, _connection, sqliteSpec, _logger);

            var tableInitializer = new SchemaRepository(_connection, sqliteProvider, sqliteSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
