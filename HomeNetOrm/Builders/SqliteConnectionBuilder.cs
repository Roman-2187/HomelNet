using System.Data.Common;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.DbTableInitializer;

namespace HomeNetOrm.Builders
{
    public class SqliteConnectionBuilder : IDbConnectionBuilder
    {
        private readonly DbConnection _connection;
        private readonly ILogger _logger;

        // Реализуем интерфейс — просто отдаем наружу наш приватный шлейф
        public DbConnection Connection => _connection;

        public SqliteConnectionBuilder(DbConnection connection, ILogger logger)
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

            var sqliteSpec = new SqliteProviderSpecification();
            var sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);
            var sqliteSchemaInit = new SqliteSchemaInitializer(sqliteAdapter, _logger);
            var sqliteProvider = new GenericSchemaProvider(sqliteSchemaInit, _connection, sqliteSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, sqliteProvider, sqliteAdapter, sqliteSchemaInit, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
