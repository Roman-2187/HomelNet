using System.Data.Common;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DbTableInitializer;

namespace HomeNetOrm.Builders
{
    public class PostgresConnectionBuilder : IDbConnectionBuilder
    {
        private readonly DbConnection _connection;
        private readonly ILogger _logger;

        // Реализуем интерфейс — отдаем центральное подключение наружу
        public DbConnection Connection => _connection;

        public PostgresConnectionBuilder(DbConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
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
    }
}
