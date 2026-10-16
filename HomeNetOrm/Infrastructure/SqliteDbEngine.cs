using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class SqliteDbEngine : IDbEngine
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _sqliteAdapter;

        public DbConnection Connection => _connection;

        public SqliteDbEngine(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            var sqliteSpec = new SqliteProviderSpecification();
            _sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);
        }

        // 🔥 КРАСОТА: Фабричные методы, которые выдают клиенту готовые репозитории, 
        // пряча внутренние генераторы под капотом движка!
        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            var generator = new SqliteUserSqlGenerator(_sqliteAdapter, _logger);
            return new UserRepository(_connection, generator, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            var generator = new SqliteMessageSqlGenerator(_sqliteAdapter, _logger);
            return new MessageRepository(_connection, generator, eventBus);
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

            var tableInitializer = new DBInitializer(_connection, sqliteProvider, _sqliteAdapter, sqliteSchemaInit, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
