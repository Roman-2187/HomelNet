using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using System.Data.Common;

namespace HomeNetOrm.Builders
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
            // 🔥 ТЕПЕРЬ ДВИЖОК ОЖИВАЕТ ТАМ, ГДЕ ДОЛЖЕН! Сама база будит свои батарейки!
            SQLitePCL.Batteries.Init();

            _postgresConnectionString = postgresConn ?? throw new ArgumentNullException(nameof(postgresConn));
            _sqliteConnectionString = sqliteConn ?? throw new ArgumentNullException(nameof(sqliteConn));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Центральная инициализация при старте приложения: теперь ПРИОРИТЕТ НА SQLite! 🎯
        public async Task InitializeAsync(DatabaseType databaseType)
        {
            // Насильно форсируем SQLite для локальной разработки, 
            // либо используем переданный тип, если вы явно запрашиваете SQLite при старте
            var targetType = databaseType == DatabaseType.PostGreSQL ? DatabaseType.SQLite : databaseType;

            try
            {
                await SwitchDatabaseAsync(targetType);
            }
            catch (DbException) when (targetType == DatabaseType.SQLite)
            {
                _logger.LogWarning("Критическая ошибка локального SQLite при старте! Аварийно пробуем PostgreSQL...");

                // Если локальный файл заблокирован или поврежден, пытаемся уйти на Postgres
                await SwitchDatabaseAsync(DatabaseType.PostGreSQL);
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
            var (connection, sqlInit, schemaProvider, 
                schemaAdapter) = builder.CreateCoreInfrastructure(databaseType);

            Connection = connection;

            // Открываем новый сетевой шлейф
            if (Connection.State != System.Data.ConnectionState.Open)
            {
                await Connection.OpenAsync();
            }

            // 3. ПЕРЕЗАПУСКАЕМ СКРИПТ ПРОВЕРКИ И ОБНОВЛЕНИЯ СТРУКТУРЫ ТАБЛИЦ! 🔄🧼
            var dbInitializer = new DBInitializer(Connection,
                schemaProvider, schemaAdapter, sqlInit, sqlInit, _logger);
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

        // Теперь этот метод защищает выполнение, опираясь на SQLite как на основную базу! 🛡
        public async Task<T?> ExecuteWithFallbackAsync<T>(Func<DbContextContainer, Task<T>> databaseOperation)
        {
            try
            {
                // Если наше основное соединение с SQLite отвалилось (например, файл заблокирован другим процессом)
                if (CurrentType == DatabaseType.SQLite &&
                    (Connection.State == System.Data.ConnectionState.Closed || Connection.State == System.Data.ConnectionState.Broken))
                {
                    _logger.LogWarning("Обнаружен разрыв соединения с SQLite перед выполнением. Переключаемся на PostgreSQL...");
                    await SwitchDatabaseAsync(DatabaseType.PostGreSQL);
                }

                return await databaseOperation(this);
            }
            catch (DbException) when (CurrentType == DatabaseType.SQLite)
            {
                _logger.LogError("Критическая ошибка SQLite. Аварийное переключение на PostgreSQL...");

                try
                {
                    // Переключаем тумблер на резервный Postgres
                    await SwitchDatabaseAsync(DatabaseType.PostGreSQL);

                    // Повторяем операцию уже на нем
                    return await databaseOperation(this);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Не удалось переключиться на резервную базу PostgreSQL!");
                    _logger.LogError($"[ВНИМАНИЕ] Ошибка фоллбэка: {ex}. Приложение продолжает работу вслепую.");
                    return default(T?);
                }
            }
        }
    }
}
