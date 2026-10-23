using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DBProviders.Postgres.SincedGenerators;
using HomeNetOrm.DBProviders.Postgres.SyncedGenerators;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Infrastructure
{
    public class SincedPostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;

        public DbConnection Connection => _connection;

        public SincedPostgresDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            // 🔥 УБРАЛИ СОЗДАНИЕ КЛАССА АДАПТЕРА ИЗ КОНСТРУКТОРА НАХЕР
        }

        public IUserRepository CreateUserRepository()
        {
            // 🔥 ФИКС: Полный автомат без адаптеров
            var generator = new SyncedPostgresUserSqlGenerator(_schemas, _logger);
            return new UserRepository(_connection, generator, _logger);
        }

        public IMessageRepository CreateMessageRepository()
        {
            // 🔥 ФИКС: Полный автомат без адаптеров
            var generator = new SyncedPostgresMessageSqlGenerator(_schemas, _logger);
            return new MessageRepository(_connection, generator, _logger);
        }

        public IDeviceSyncRepository CreateDeviceSyncRepository()
        {
            // 🔥 ФИКС: Наш новый генератор теперь наследник ядра и требует _schemas и _logger!
            // А хардкодная строка "device_sync" идет нахер, так как имя таблицы он найдет сам из Schema!
            var generator = new PostgresDeviceSyncSqlGenerator(_schemas, _logger);
            return new DeviceSyncRepository(_connection, generator);
        }

        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var postgresSpec = new PostgresProviderSpecification();

            // 🔥 ФИКС: Убрали прослойки адаптеров
            var postgresSchemaInit = new PostgresSchemaInitializer(postgresSpec, _logger);
            var postgresProvider = new GenericSchemaProvider(postgresSchemaInit, _connection, postgresSpec, _logger);

            var tableInitializer = new DBInitializer(_connection, postgresProvider, postgresSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
