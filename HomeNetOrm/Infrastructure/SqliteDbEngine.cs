using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class SqliteDbEngine : IDbEngine
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ILogger _logger;
        private readonly Dictionary<Type, object> _generators = new();

        public DbConnection Connection => _connection;

        public SqliteDbEngine(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            // Скрываем кишки генераторов прямо в конструкторе движка
            var sqliteSpec = new SqliteProviderSpecification();
            var sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);

            _generators[typeof(UserEntity)] = new SqliteUserSqlGenerator(sqliteAdapter,logger);
            _generators[typeof(MessageEntity)] = new SqliteMessageSqlGenerator(sqliteAdapter, logger);
            _generators[typeof(FriendEntity)] = new SqliteFriendSqlGenerator(sqliteAdapter, logger);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var sqliteSpec = new SqliteProviderSpecification();
            var sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);
            var sqliteSchemaInit = new SqliteSchemaInitializer(sqliteAdapter, _logger);
            var sqliteProvider = new GenericSchemaProvider(sqliteSchemaInit, _connection, sqliteSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, sqliteProvider, sqliteAdapter, sqliteSchemaInit, _logger);
            await tableInitializer.InitializeAsync();
        }

        public ISqlGenerator<T> GetGenerator<T>() where T : class
        {
            if (_generators.TryGetValue(typeof(T), out var gen))
                return (ISqlGenerator<T>)gen;

            throw new KeyNotFoundException($"SQLite генератор для {typeof(T).Name} не найден.");
        }
    }
}
