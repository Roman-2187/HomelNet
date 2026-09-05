using HomeNetCore.Data.Builders;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Models;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetCore.Data
{
    public class DbContextContainer : IAsyncDisposable
    {
        private readonly string _postgresConnectionString;
        private readonly string _sqliteConnectionString;
        private readonly ILogger _logger;

        public DbConnection Connection { get; private set; } = null!;
        public ISqlGenerator<UserEntity> UserSqlGen { get; private set; } = null!;
        public ISqlGenerator<MessageEntity> MessageSqlGen { get; private set; } = null!;
        public ISqlGenerator<FriendEntity> FriendSqlGen { get; private set; } = null!;
        public DatabaseType CurrentType { get; private set; }

        public DbContextContainer(string postgresConn, string sqliteConn, ILogger logger)
        {
            _postgresConnectionString = postgresConn ?? throw new ArgumentNullException(nameof(postgresConn));
            _sqliteConnectionString = sqliteConn ?? throw new ArgumentNullException(nameof(sqliteConn));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Центральная инициализация при старте приложения
        public async Task InitializeAsync(DatabaseType databaseType)
        {
            await SwitchDatabaseAsync(databaseType);
        }

        // 🔥 МАГИЧЕСКИЙ ТУМБЛЕР ПЕРЕКЛЮЧЕНИЯ НА ЛЕТУ!
        public async Task SwitchDatabaseAsync(DatabaseType databaseType)
        {
            _logger.LogInformation($"Переключение инфраструктуры СУБД на {databaseType}...");

            // 1. Утилизируем старое подключение, если оно было открыто
            if (Connection != null)
            {
                _logger.LogInformation("Закрытие старого соединения базы данных...");
                await Connection.CloseAsync();
                await Connection.DisposeAsync();
            }

            CurrentType = databaseType;
            string targetConnectionString = databaseType == DatabaseType.PostGreSQL
                ? _postgresConnectionString
                : _sqliteConnectionString;

            // 2. Юзаем фабрику-строитель для сборки новой инфраструктуры
            var builder = new DatabaseInfrastructureBuilder(targetConnectionString, _logger);
            var (connection, sqlInit, schemaProvider, schemaAdapter) = builder.CreateCoreInfrastructure(databaseType);

            Connection = connection;

            // Открываем новый сетевой шлейф
            if (Connection.State != System.Data.ConnectionState.Open)
            {
                await Connection.OpenAsync();
            }

            // 3. ПЕРЕЗАПУСКАЕМ СКРИПТ ПРОВЕРКИ И ОБНОВЛЕНИЯ СТРУКТУРЫ ТАБЛИЦ! 🔄🧼
            var dbInitializer = new DBInitializer(Connection, schemaProvider, schemaAdapter, sqlInit, sqlInit, _logger);
            await dbInitializer.InitializeAsync();

            // 4. Перевыпекаем генераторы запросов под новую СУБД
            UserSqlGen = builder.CreateSqlGenerator<UserEntity>(databaseType, schemaAdapter);
            MessageSqlGen = builder.CreateSqlGenerator<MessageEntity>(databaseType, schemaAdapter);
            FriendSqlGen = builder.CreateSqlGenerator<FriendEntity>(databaseType, schemaAdapter);

            _logger.LogInformation($"База данных {databaseType} успешно перестроена и готова к работе.");
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null) await Connection.DisposeAsync();
        }
    }
}

