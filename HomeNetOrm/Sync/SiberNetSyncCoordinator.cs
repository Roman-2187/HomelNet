namespace HomeNetOrm.Sync
{
    using Dapper;
    using global::HomeNetOrm.Interfaces;
    using global::HomeNetOrm.Interfaces.HomeNetOrm.Interfaces;
    using HomeNetCore.Extensions;
    using HomeNetCore.Interfaces.Diagnostics;
    using HomeNetCore.Interfaces.Events;
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;

    namespace HomeNetOrm.Sync
    {
        /// <summary>
        /// Реализация координатора синхронизации SiberNet ОРМ-слоя.
        /// Изолированно синхронизирует базы данных между собой в обход бизнес-сервисов.
        /// </summary>
        public class SiberNetSyncCoordinator : ISiberNetSyncCoordinator, IDisposable
        {
            private readonly IDbConnectionBuilder _sqliteBuilder;
            private readonly IDbConnectionBuilder _postgresBuilder;
            private readonly IDbContextContainer _contextContainer;
            private readonly IEventBus _eventBus;
            private readonly ILogger _logger;

            private bool _isInitialSyncExecuted;

            public SiberNetSyncCoordinator(
                IDbConnectionBuilder sqliteBuilder,
                IDbConnectionBuilder postgresBuilder,
                IDbContextContainer contextContainer,
                IEventBus eventBus,
                ILogger logger)
            {
                _sqliteBuilder = sqliteBuilder ?? throw new ArgumentNullException(nameof(sqliteBuilder));
                _postgresBuilder = postgresBuilder ?? throw new ArgumentNullException(nameof(postgresBuilder));
                _contextContainer = contextContainer ?? throw new ArgumentNullException(nameof(contextContainer));
                _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                // 🔌 Подписываемся на наши понятные ОРМ-рекорды мутаций
                _eventBus.Subscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
                _eventBus.Subscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
                _eventBus.Subscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);

                _eventBus.Subscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
                _eventBus.Subscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
                _eventBus.Subscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
            }

            public async Task StartAsync()
            {
                if (_isInitialSyncExecuted) return;
                _isInitialSyncExecuted = true;

                if (!_contextContainer.IsPostgresAvailable)
                {
                    _logger.LogWarning("[СИНХРОНИЗАТОР] Центральный PostgreSQL недоступен. Первоначальный Pull пропущен.");
                    return;
                }

                _logger.LogInfo("=== [СИНХРОНИЗАТОР] ЗАПУСК ПЕРВОНАЧАЛЬНОЙ СВЕРКИ (PULL FROM POSTGRES) ===");
                // Логика выкачивания старых данных при старте (с использованием чистых подключений из билдеров)
            }

            // =================================================================
            // 🛠 ОБРАБОТЧИКИ РЕАЛТАЙМ-РЕПЛИКАЦИИ ПОЛЬЗОВАТЕЛЕЙ
            // =================================================================

            private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация нового пользователя {msg.User.Id} в Postgres...");
                    // Твой чистый инсерт в Postgres через Dapper на лету
                    const string sql = "INSERT INTO users (id, first_name, last_name, email, is_synced) VALUES (@Id, @FirstName, @LastName, @Email, 1);";
                    await pg.ExecuteAsync(sql, msg.User);
                }, "User Insert");

            private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация обновления пользователя {msg.User.Id} в Postgres...");
                    const string sql = "UPDATE users SET first_name = @FirstName, last_name = @LastName, is_synced = 1 WHERE id = @Id;";
                    await pg.ExecuteAsync(sql, msg.User);
                }, "User Update");

            private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация удаления пользователя {msg.UserId} в Postgres...");
                    await pg.ExecuteAsync("DELETE FROM users WHERE id = @Id", new { Id = msg.UserId });
                }, "User Delete");

            // =================================================================
            // 🛠 ОБРАБОТЧИКИ РЕАЛТАЙМ-РЕПЛИКАЦИИ СООБЩЕНИЙ
            // =================================================================

            private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация сообщения {msg.Message.Id} в центральный Postgres...");
                    const string sql = "INSERT INTO messages (id, sender_id, receiver_id, text_content, is_synced) VALUES (@Id, @SenderId, @ReceiverId, @TextContent, 1);";
                    await pg.ExecuteAsync(sql, msg.Message);
                }, "Message Insert");

            private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация статуса сообщения {msg.Message.Id} в Postgres...");
                    const string sql = "UPDATE messages SET is_read = @IsRead, is_synced = 1 WHERE id = @Id;";
                    await pg.ExecuteAsync(sql, msg.Message);
                }, "Message Update");

            private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg) =>
                ExecuteSyncTask(async (pg) =>
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация удаления сообщения {msg.MessageId} в Postgres...");
                    await pg.ExecuteAsync("DELETE FROM messages WHERE id = @Id", new { Id = msg.MessageId });
                }, "Message Delete");

            // =================================================================
            // 🛡️ ЗАСТРАХОВАННЫЙ КОНВЕЙЕР ВЫПОЛНЕНИЯ ЗАДАЧ СИНХРОНИЗАЦИИ
            // =================================================================
            private void ExecuteSyncTask(Func<DbConnection, Task> syncAction, string operationName)
            {
                if (!_contextContainer.IsPostgresAvailable) return;

                Task.Run(async () =>
                {
                    try
                    {
                        // 🎯 ИСПРАВЛЕНО: Забираем живое центральное подключение прямо у билдера Postgres!
                        var pgConnection = _postgresBuilder.Connection;

                        if (pgConnection.State != System.Data.ConnectionState.Open)
                        {
                            await pgConnection.OpenAsync();
                        }

                        // Выполняем репликацию на лету в фоновом пуле
                        await syncAction(pgConnection);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[СИНХРОНИЗАТОР] Сбой операции '{operationName}' при пуше в Postgres: {ex.Message}");
                    }
                });
            }


            public void Dispose()
            {
                // Корректная отписка от событий шины при уничтожении объекта
            }
        }
    }

}
