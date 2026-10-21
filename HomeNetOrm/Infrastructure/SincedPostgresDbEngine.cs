using System.Data.Common;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.DBProviders.Postgres.SyncedGenerators;

namespace HomeNetOrm.Infrastructure
{
    public class SincedPostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _postgresAdapter;
        private readonly IEnumerable<TableSchema> _schemas;

        public DbConnection Connection => _connection;

        public SincedPostgresDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            var postgresSpec = new PostgresProviderSpecification();
            _postgresAdapter = new GenericSchemaAdapter(postgresSpec);
        }

        public IUserRepository CreateUserRepository()
        {
            // 🔥 ПОДМЕНА: Создаем синхронизированного наследника вместо стерильной базы
            var generator = new SyncedPostgresUserSqlGenerator(_postgresAdapter, _schemas, _logger);
            return new UserRepository(_connection, generator,_logger);
        }

        public IMessageRepository CreateMessageRepository()
        {
            // 🔥 ПОДМЕНА: Создаем синхронизированного наследника для сообщений
            var generator = new SyncedPostgresMessageSqlGenerator(_postgresAdapter, _schemas, _logger);
            return new MessageRepository(_connection, generator,_logger);
        }

        public IDeviceSyncRepository CreateDeviceSyncRepository()
        {
            var generator = new PostgresDeviceSyncSqlGenerator();

            // === ИСПРАВЛЕНИЕ: Передаем имя таблицы сверху из DI-движка! 🎯 ===
            return new DeviceSyncRepository(_connection, generator, "device_sync");
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

            var tableInitializer = new DBInitializer(_connection, postgresProvider, _postgresAdapter, postgresSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
