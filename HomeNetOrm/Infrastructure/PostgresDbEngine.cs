using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Repositories; 
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class PostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _postgresAdapter;

        public DbConnection Connection => _connection;

        public PostgresDbEngine(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            var postgresSpec = new PostgresProviderSpecification();
            _postgresAdapter = new GenericSchemaAdapter(postgresSpec);
        }

        // 🔥 ФАБРИКА ДЛЯ СЕРВЕРА: Создает репозитории без автобуса событий (вызовется конструктор №2)
        public IUserRepository CreateUserRepository()
        {
            var generator = new PostgresUserSqlGenerator(_postgresAdapter, _logger);
            return new UserRepository(_connection, generator);
        }

        public IMessageRepository CreateMessageRepository()
        {
            var generator = new PostgresMessageSqlGenerator(_postgresAdapter, _logger);
            return new MessageRepository(_connection, generator);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var postgresSpec = new PostgresProviderSpecification();
            var postgresSchemaInit = new PostgresSchemaInitializer(_postgresAdapter, _logger);
            var postgresProvider = new GenericSchemaProvider(postgresSchemaInit, _connection, postgresSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, postgresProvider, _postgresAdapter, postgresSchemaInit, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
