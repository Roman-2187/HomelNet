using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.DBProviders.Sqlite.SincedGenerators;
using HomeNetOrm.DBProviders.Sqlite.SyncedGenerators;
using HomeNetOrm.Interfaces.Builders;
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

        public DbConnection Connection => _connection;

        public SincedSqliteDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            // 🔥 АДАПТЕР ТУТ БОЛЬШЕ НЕ НУЖЕН, ОН СТАЛ ПОЛНОСТЬЮ СТАТИЧЕСКИМ
        }

        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            // 🔥 ФИКС: Генераторы больше не принимают адаптер!
            var generator = new SyncedSqliteUserSqlGenerator(_schemas, _logger);
            return new UserRepository(_connection, generator, _logger, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            // 🔥 ФИКС: Очистили конструктор
            var generator = new SyncedSqliteMessageSqlGenerator(_schemas, _logger);
            return new MessageRepository(_connection, generator, _logger, eventBus);
        }

        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            // 🔥 ФИКС: Убрали адаптер из параметров
            var generator = new SqliteDeviceSettingsSqlGenerator(_schemas, _logger);
            return new ClientDeviceSettingsRepository(_connection, generator);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var sqliteSpec = new SqliteProviderSpecification();

            // 🔥 ФИКС: Инициализатор и провайдер принимают спецификацию напрямую без адаптеров!
            var sqliteSchemaInit = new SqliteSchemaInitializer(sqliteSpec, _logger);
            var sqliteProvider = new GenericSchemaProvider(sqliteSchemaInit, _connection, sqliteSpec, _logger);

            var tableInitializer = new SchemaRepository(_connection, sqliteProvider, sqliteSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
