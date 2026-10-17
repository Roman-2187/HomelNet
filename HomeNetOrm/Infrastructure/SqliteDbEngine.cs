using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite;
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
    public class SqliteDbEngine : IDbEngine
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ILogger _logger;
        private readonly GenericSchemaAdapter _sqliteAdapter;
        private readonly IEnumerable<TableSchema> _schemas; // 🎯 Локальное поле для хранения внешних схем

        public DbConnection Connection => _connection;

        // 🎯 Конструктор теперь третьим параметром принимает схемы
        public SqliteDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas)); // Запоминаем чертежи таблиц
            _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);

            var sqliteSpec = new SqliteProviderSpecification();
            _sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);
        }

        // 🔥 КРАСОТА: Передаем _schemas внутрь генератора SQLite
        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            var generator = new SqliteUserSqlGenerator(_sqliteAdapter, _schemas, _logger); // 🎯 Пробросили схемы
            return new UserRepository(_connection, generator, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            var generator = new SqliteMessageSqlGenerator(_sqliteAdapter, _schemas, _logger); // 🎯 Пробросили схемы
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

            // 🎯 Передаем _schemas пятым параметром в сверщик таблиц DBInitializer
            var tableInitializer = new DBInitializer(_connection, sqliteProvider, _sqliteAdapter, sqliteSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();
        }
    }
}
