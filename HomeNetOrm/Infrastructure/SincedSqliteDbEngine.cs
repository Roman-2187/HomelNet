using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.DBProviders.Sqlite.SyncedGenerators;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class SincedSqliteDbEngine : IDbEngine
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _sqliteAdapter;
        private readonly IEnumerable<TableSchema> _schemas;

        public DbConnection Connection => _connection;

        public SincedSqliteDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            var sqliteSpec = new SqliteProviderSpecification();
            _sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);
        }

       


        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            // 🔥 ПОДМЕНЯЕМ СТЕРИЛЬНЫЙ НА СИНХРОНИЗИРОВАННЫЙ! 🔥
            // Вместо SqliteUserSqlGenerator ставим SyncedSqliteUserSqlGenerator
            var generator = new SyncedSqliteUserSqlGenerator(_sqliteAdapter, _schemas, _logger);

            return new UserRepository(_connection, generator, eventBus);
        }


        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            // 🔥 ПОДМЕНА: Включаем синхронизацию сообщений "из коробки" через Synced-наследника!
            var generator = new SyncedSqliteMessageSqlGenerator(_sqliteAdapter, _schemas, _logger);
            return new MessageRepository(_connection, generator, eventBus);
        }

        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            var generator = new SqliteDeviceSettingsSqlGenerator();
            return new ClientDeviceSettingsRepository(_connection, generator);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var sqliteSpec = new SqliteProviderSpecification();
            var sqliteSchemaInit = new SqliteSchemaInitializer(_sqliteAdapter, _logger);
            var sqliteProvider = new GenericSchemaProvider(sqliteSchemaInit, _connection, sqliteSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, sqliteProvider, _sqliteAdapter, sqliteSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
