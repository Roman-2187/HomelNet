using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Models;
using System.Data.Common;

namespace HomeNetCore.Data
{
    public class DbContextContainer : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        // Единственный источник правды для подключений и генераторов запросов
        public DbConnection Connection { get; private set; } = null!;
        public ISqlGenerator<UserEntity> UserSqlGen { get; private set; } = null!;
        public ISqlGenerator<MessageEntity> MessageSqlGen { get; private set; } = null!;
        public ISqlGenerator<FriendEntity> FriendSqlGen { get; private set; } = null!;

        public DbContextContainer(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(DatabaseType databaseType)
        {
            _logger.LogInformation("Запуск инициализации инфраструктуры БД...");

            // 1. Используем наш переименованный Строитель Инфраструктуры 🏭
            var builder = new DatabaseInfrastructureBuilder(_connectionString, _logger);

            var (connection, sqlInit, schemaProvider, schemaAdapter) = builder.CreateCoreInfrastructure(databaseType);
            Connection = connection;

            if (Connection.State != System.Data.ConnectionState.Open)
            {
                await Connection.OpenAsync();
            }

            // 2. Автопилот проверки таблиц Postgres/SQLite
            var dbInitializer = new DBInitializer(Connection, schemaProvider, schemaAdapter, sqlInit, sqlInit, _logger);
            await dbInitializer.InitializeAsync();

            // 3. Выпекаем дженерик-генераторы запросов под сущности ядра
            UserSqlGen = builder.CreateSqlGenerator<UserEntity>(databaseType, schemaAdapter);
            MessageSqlGen = builder.CreateSqlGenerator<MessageEntity>(databaseType, schemaAdapter);
            FriendSqlGen = builder.CreateSqlGenerator<FriendEntity>(databaseType, schemaAdapter);

            _logger.LogInformation("Инфраструктура хранения данных успешно инициализирована.");
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync();
            }
        }
    }
}


