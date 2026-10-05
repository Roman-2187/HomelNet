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
        // Центральная инициализация при старте приложения: полная свобода выбора без хардкода! 🔄🎯
        public async Task InitializeAsync(DatabaseType databaseType)
        {
            try
            {
                // Что передали снаружи в параметре — то железно и запускаем!
                await SwitchDatabaseAsync(databaseType);
            }
            catch (DbException ex)
            {
                _logger.LogError($"[КРИТ] Не удалось инициализировать СУБД {databaseType} при старте! Ошибка: {ex.Message}");

                // Аварийный фоллбэк: если запрашивали одну базу и она упала, 
                // пробуем автоматически переключиться на альтернативу, чтобы приложение не легло
                try
                {
                    DatabaseType backupType = databaseType == DatabaseType.PostGreSQL
                        ? DatabaseType.SQLite
                        : DatabaseType.PostGreSQL;

                    _logger.LogWarning($"Аварийно пробуем переключиться на резервную инфраструктуру {backupType}...");
                    await SwitchDatabaseAsync(backupType);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical($"[КАТАСТРОФА] Резервная СУБД тоже недоступна! Ошибка: {fallbackEx.Message}");
                }
            }
        }


        // 🔥 МАГИЧЕСКИЙ ТУМБЛЕР ПЕРЕКЛЮЧЕНИЯ НА ЛЕТУ!
        public async Task SwitchDatabaseAsync(DatabaseType databaseType)
        {
            _logger.LogInfo($"Переключение инфраструктуры СУБД на {databaseType}...");

            // 1. Утилизируем старое подключение, если оно было открыто
            if (Connection != null)
            {
                _logger.LogInfo("Закрытие старого соединения базы данных...");
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

            _logger.LogInfo($"База данных {databaseType} успешно перестроена и готова к работе.");
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null) await Connection.DisposeAsync();
        }

        // Теперь этот метод защищает выполнение, опираясь на SQLite как на основную базу! 🛡
        // Симметричный защитник выполнения: спасает приложение при падении ЛЮБОЙ из двух СУБД! 🛡🔄
        public async Task<T?> ExecuteWithFallbackAsync<T>(Func<DbContextContainer, Task<T>> databaseOperation)
        {
            try
            {
                // Проверяем живое ли соединение перед выполнением
                if (Connection.State == System.Data.ConnectionState.Closed || Connection.State == System.Data.ConnectionState.Broken)
                {
                    DatabaseType backupType = CurrentType == DatabaseType.PostGreSQL ? DatabaseType.SQLite : DatabaseType.PostGreSQL;
                    _logger.LogWarning($"Соединение с {CurrentType} разорвано перед операцией. Пробуем переключиться на {backupType}...");
                    await SwitchDatabaseAsync(backupType);
                }

                return await databaseOperation(this);
            }
            catch (DbException ex)
            {
                // Вычисляем, на какую базу уходить в случае аварии
                DatabaseType fallbackType = CurrentType == DatabaseType.PostGreSQL ? DatabaseType.SQLite : DatabaseType.PostGreSQL;

                _logger.LogError($"Критическая ошибка текущей СУБД {CurrentType}: {ex.Message}. Аварийно переключаемся на {fallbackType}...");

                try
                {
                    // Переключаем тумблер на резервную базу
                    await SwitchDatabaseAsync(fallbackType);

                    // Повторяем операцию уже на резервной инфраструктуре
                    return await databaseOperation(this);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical($"[КАТАСТРОФА] Не удалось выполнить операцию даже на резервной базе {fallbackType}!");
                    _logger.LogError($"Ошибка фоллбэка: {fallbackEx}. Приложение продолжает работу вслепую.");
                    return default(T?);
                }
            }
        }

    }
}
