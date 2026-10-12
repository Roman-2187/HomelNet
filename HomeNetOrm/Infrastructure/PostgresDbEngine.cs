using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Builders;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class PostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly Dictionary<Type, object> _generators = new();

        public DbConnection Connection => _connection;

        public PostgresDbEngine(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            // Настраиваем Postgres диалект внутри движка
            var postgresSpec = new PostgresProviderSpecification();
            var postgresAdapter = new GenericSchemaAdapter(postgresSpec);

            _generators[typeof(UserEntity)] = new PostgresUserSqlGenerator(postgresAdapter,logger);
            _generators[typeof(MessageEntity)] = new PostgresMessageSqlGenerator(postgresAdapter, logger);
            _generators[typeof(FriendEntity)] = new PostgresFriendSqlGenerator(postgresAdapter, logger);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var postgresSpec = new PostgresProviderSpecification();
            var postgresAdapter = new GenericSchemaAdapter(postgresSpec);
            var postgresSchemaInit = new PostgresSchemaInitializer(postgresAdapter, _logger);
            var postgresProvider = new GenericSchemaProvider(postgresSchemaInit, _connection, postgresSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, postgresProvider, postgresAdapter, postgresSchemaInit, _logger);
            await tableInitializer.InitializeAsync();
        }

        public ISqlGenerator<T> GetGenerator<T>() where T : class
        {
            if (_generators.TryGetValue(typeof(T), out var gen))
                return (ISqlGenerator<T>)gen;

            throw new KeyNotFoundException($"Postgres генератор для {typeof(T).Name} не найден.");
        }
    }
}

