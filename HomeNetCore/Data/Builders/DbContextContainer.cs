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
            try
            {
                await SwitchDatabaseAsync(databaseType);
            }
            catch (DbException ) when (databaseType == DatabaseType.PostGreSQL)
            {
                _logger.LogWarning("Не удалось подключиться к PostgreSQL при старте. Аварийно переключаемся на SQLite...");

                // Если Postgres лежит прямо на старте, разворачиваем инфраструктуру SQLite
                await SwitchDatabaseAsync(DatabaseType.SQLite);
            }
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



        public async Task<T?> ExecuteWithFallbackAsync<T>(Func<DbContextContainer, Task<T>> databaseOperation)
        {
            try
            {
                // Проверяем, если соединение закрыто/сломано из-за убитого процесса, сразу переключаем
                if (CurrentType == DatabaseType.PostGreSQL &&
                    (Connection.State == System.Data.ConnectionState.Closed || Connection.State == System.Data.ConnectionState.Broken))
                {
                    _logger.LogWarning("Обнаружен разрыв соединения с PostgreSQL перед выполнением. Переключаемся на SQLite...");
                    await SwitchDatabaseAsync(DatabaseType.SQLite);
                }

                return await databaseOperation(this);
            }
            catch (DbException ) when (CurrentType == DatabaseType.PostGreSQL)
            {
                _logger.LogError("Критическая ошибка PostgreSQL (возможно, процесс был убит). Аварийное переключение на SQLite...");

                try
                {
                    // Переключаем тумблер на SQLite
                    await SwitchDatabaseAsync(DatabaseType.SQLite);

                    // Повторяем операцию уже на новой базе
                    return await databaseOperation(this);
                }
                catch (Exception ех)
                {
                    _logger.LogCritical("Не удалось переключиться на резервную базу SQLite!");
                    _logger.LogError($"[ВНИМАНИЕ] Не удалось выполнить проверку или обновление структуры таблиц для СУБД {ех}. Приложение продолжает запуск на страх и риск.");
                        return default(T?);
                }
            }
        }

    }
}

