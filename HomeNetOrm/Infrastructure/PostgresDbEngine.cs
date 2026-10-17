using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Repositories;
using HomeNetOrm.Models; // 🎯 Подключаем пространство имен для TableSchema
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Infrastructure
{
    public class PostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _postgresAdapter;
        private readonly IEnumerable<TableSchema> _schemas; // 🎯 Локальное поле для хранения внешних схем

        public DbConnection Connection => _connection;

        // 🎯 Конструктор теперь третьим параметром принимает схемы
        public PostgresDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas)); // Запоминаем чертежи таблиц
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            var postgresSpec = new PostgresProviderSpecification();
            _postgresAdapter = new GenericSchemaAdapter(postgresSpec);
        }

        // 🔥 ФАБРИКА ДЛЯ СЕРВЕРА: Передаем _schemas внутрь генератора
        public IUserRepository CreateUserRepository()
        {
            var generator = new PostgresUserSqlGenerator(_postgresAdapter, _schemas, _logger); // 🎯 Пробросили схемы
            return new UserRepository(_connection, generator);
        }

        public IMessageRepository CreateMessageRepository()
        {
            var generator = new PostgresMessageSqlGenerator(_postgresAdapter, _schemas, _logger); // 🎯 Пробросили схемы
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

            // 🎯 Передаем _schemas пятым параметром в сверщик таблиц DBInitializer
            var tableInitializer = new DBInitializer(_connection, postgresProvider, _postgresAdapter, postgresSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
