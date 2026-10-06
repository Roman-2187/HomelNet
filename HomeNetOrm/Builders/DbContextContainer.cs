using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events; // 🔥 Подключаем твою шину событий
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
        private readonly IEventBus _eventBus; // 🔥 Наша шина

        public DbConnection Connection { get; private set; } = null!;
        public ISqlGenerator<UserEntity> UserSqlGen { get; private set; } = null!;
        public ISqlGenerator<MessageEntity> MessageSqlGen { get; private set; } = null!;
        public ISqlGenerator<FriendEntity> FriendSqlGen { get; private set; } = null!;
        public DatabaseType CurrentType { get; private set; }

        // ✂️ СНЕСЛИ НАХУЙ ВЕСЬ ГЕМОРРОЙ С БУЛЕВЫМ ФЛАГОМ IsReady!

        public DbContextContainer(string postgresConn, string sqliteConn, IEventBus eventBus, ILogger logger)
        {
            SQLitePCL.Batteries.Init();

            _postgresConnectionString = postgresConn ?? throw new ArgumentNullException(nameof(postgresConn));
            _sqliteConnectionString = sqliteConn ?? throw new ArgumentNullException(nameof(sqliteConn));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus)); //
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(DatabaseType databaseType)
        {
            try
            {
                await SwitchDatabaseAsync(databaseType);
            }
            catch (DbException ex)
            {
                _logger.LogError($"[КРИТ] Не удалось инициализировать СУБД {databaseType} при старте! Ошибка: {ex.Message}");

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

        public async Task SwitchDatabaseAsync(DatabaseType databaseType)
        {
            _logger.LogInfo($"Переключение инфраструктуры СУБД на {databaseType}...");

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

            var builder = new DatabaseInfrastructureBuilder(targetConnectionString, _logger);
            var (connection, sqlInit, schemaProvider,
                schemaAdapter) = builder.CreateCoreInfrastructure(databaseType);

            Connection = connection;

            if (Connection.State != System.Data.ConnectionState.Open)
            {
                await Connection.OpenAsync();
            }

            // Передаем шину в инициализатор
            var dbInitializer = new DBInitializer(Connection,
                schemaProvider, schemaAdapter, sqlInit, sqlInit, _eventBus, _logger);
            await dbInitializer.InitializeAsync();

            UserSqlGen = builder.CreateSqlGenerator<UserEntity>(databaseType, schemaAdapter);
            MessageSqlGen = builder.CreateSqlGenerator<MessageEntity>(databaseType, schemaAdapter);
            FriendSqlGen = builder.CreateSqlGenerator<FriendEntity>(databaseType, schemaAdapter);

            _logger.LogInfo($"База данных {databaseType} успешно перестроена и готова к работе.");

            // 🔥 ЗДЕСЬ МОЖНО ТОЖЕ ДУБЛИРОВАТЬ, НО МЫ СДЕЛАЕМ ЭТО ВНУТРИ ИНИЦИАЛИЗАТОРА ТАБЛИЦ,
            // чтобы гарантировать окончание проверок!
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null) await Connection.DisposeAsync();
        }

        public async Task<T?> ExecuteWithFallbackAsync<T>(Func<DbContextContainer, Task<T>> databaseOperation)
        {
            try
            {
                if (Connection.State == System.Data.ConnectionState.Closed || Connection.State == System.Data.ConnectionState.Broken)
                {
                    DatabaseType backupType = CurrentType == DatabaseType.PostGreSQL ? DatabaseType.SQLite : DatabaseType.PostGreSQL;
                    _logger.LogWarning($"Сосоединение с {CurrentType} разорвано перед операцией. Пробуем переключиться на {backupType}...");
                    await SwitchDatabaseAsync(backupType);
                }

                return await databaseOperation(this);
            }
            catch (DbException ex)
            {
                DatabaseType fallbackType = CurrentType == DatabaseType.PostGreSQL ? DatabaseType.SQLite : DatabaseType.PostGreSQL;
                _logger.LogError($"Критическая ошибка текущей СУБД {CurrentType}: {ex.Message}. Аварийно переключаемся на {fallbackType}...");

                try
                {
                    await SwitchDatabaseAsync(fallbackType);
                    return await databaseOperation(this);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical($"[КАТАСТРОФА] Не удалось выполнить операцию even на резервной базе {fallbackType}!");
                    _logger.LogError($"Ошибка фоллбэка: {fallbackEx}. Приложение продолжает работу вслепую.");
                    return default(T?);
                }
            }
        }
    }
}
