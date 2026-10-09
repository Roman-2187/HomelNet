using Dapper;
using global::HomeNetOrm.Interfaces;
using global::HomeNetOrm.Interfaces.HomeNetOrm.Interfaces;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using System;
using System.Data.Common;
using System.Runtime.Intrinsics.X86;
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

            // 🔌 Подписываемся на ОРМ-рекорды мутаций для реалтайм-репликации
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
                _logger.LogWarning("[СИНХРОНИЗАТОР] Центральный PostgreSQL недоступен. Синхронизация пропущена.");
                return;
            }

            _logger.LogInfo("=== [СИНХРОНИЗАТОР] ЗАПУСК ПЕРВОНАЧАЛЬНОЙ СВЕРКИ ===");

            try
            {
                // 🔌 Открываем чистые соединения напрямую через билдеры подключений
                var sqlite = _sqliteBuilder.Connection;
                var pg = _postgresBuilder.Connection;

                if (sqlite.State != System.Data.ConnectionState.Open) await sqlite.OpenAsync();
                if (pg.State != System.Data.ConnectionState.Open) await pg.OpenAsync();

                // 🔥 ШАГ 1: PULL (Стягиваем то, что появилось на сервере)
                await PullFromPostgresAsync(sqlite, pg);

                // 🔥 ШАГ 2: PUSH (Выгружаем локальные оффлайн-изменения, включая 12 юзеров!)
                await PushLocalChangesToPostgresAsync(sqlite, pg);

                _logger.LogInfo("=== [СИНХРОНИЗАТОР] ПЕРВОНАЧАЛЬНАЯ СВЕРКА УСПЕШНО ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[СИНХРОНИЗАТОР] Катастрофа стартовой синхронизации: {ex.Message}");
            }
        }

        /// <summary>
        /// Скачивает новые записи с сервера (Postgres) в локальный кэш (SQLite)
        /// </summary>
        private async Task PullFromPostgresAsync(DbConnection sqlite, DbConnection pg)
        {
            _logger.LogInfo("[СИНХРОНИЗАТОР] Шаг PULL: Скачиваем новых пользователей с Postgres...");

            var pgUsers = await pg.QueryAsync<UserEntity>("SELECT * FROM users;");

            foreach (var pgUser in pgUsers)
            {
                var exists = await sqlite.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(1) FROM users WHERE id = @Id;", new { Id = pgUser.Id });

                if (!exists)
                {
                    _logger.LogInfo($"[СИНХРОНИЗАТОР] Найдена новая запись на сервере. Скачиваем {pgUser.Id} в SQLite...");
                    const string insertSql = "INSERT INTO users (id, first_name, last_name, phone_number, email, password, created_at, updated_at, is_synced) " +
                                             "VALUES (@Id, @FirstName, @LastName, @PhoneNumber, @Email, @Password, @CreatedAt, @UpdatedAt, 1);";
                    await sqlite.ExecuteAsync(insertSql, pgUser);
                }
            }
        }

        /// <summary>
        /// Проталкивает локальные несинхронизированные данные (is_synced = 0) на сервер (Postgres)
        /// </summary>
        private async Task PushLocalChangesToPostgresAsync(DbConnection sqlite, DbConnection pg)
        {
            _logger.LogInfo("[СИНХРОНИЗАТОР] Шаг PUSH: Выгружаем локальных пользователей (is_synced = 0) в Postgres...");

            // 🎯 ФИНТ С АЛИАСАМИ: Явно маппим snake_case из SQLite в свойства C# модели UserEntity,
            // чтобы Dapper не собирал пустые объекты с дефолтными Guid.Empty
            const string selectSql = @"
                SELECT 
                    id AS Id, 
                    first_name AS FirstName, 
                    last_name AS LastName, 
                    phone_number AS PhoneNumber, 
                    email AS Email, 
                    password AS Password, 
                    created_at AS CreatedAt, 
                    updated_at AS UpdatedAt, 
                    is_synced AS IsSynced 
                FROM users 
                WHERE is_synced = 0;";

            var localUnsyncedUsers = await sqlite.QueryAsync<UserEntity>(selectSql);

            foreach (var user in localUnsyncedUsers)
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Выгрузка оффлайн-пользователя {user.Id} ({user.FullName}) в Postgres...");

                // 🛡️ Открываем транзакцию для Postgres, чтобы гарантировать запись без зависания в буферах
                using var transaction = pg.BeginTransaction();
                try
                {
                    // Исправлено: запрашиваем количество как чистый int вместо коварного bool
                    const string checkSql = "SELECT COUNT(1) FROM users WHERE id = @Id;";
                    var count = await pg.ExecuteScalarAsync<int>(checkSql, new { Id = user.Id }, transaction);

                    if (count == 0)
                    {
                        const string insertPgSql = @"
                            INSERT INTO users 
                                (id, first_name, last_name, phone_number, email, password, created_at, updated_at, is_synced) 
                            VALUES 
                                (@Id, @FirstName, @LastName, @PhoneNumber, @Email, @Password, @CreatedAt, @UpdatedAt, 1);";

                        await pg.ExecuteAsync(insertPgSql, user, transaction);
                    }

                    // Обновляем флаг синхронизации локально в SQLite
                    const string updateSql = "UPDATE users SET is_synced = 1 WHERE id = @Id;";
                    await sqlite.ExecuteAsync(updateSql, new { Id = user.Id });

                    // 🔥 Фиксируем изменения в Postgres на диске
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError($"[СИНХРОНИЗАТОР] Сбой выгрузки пользователя {user.Id}: {ex.Message}");
                    throw; // Пробрасываем выше, чтобы остановить цикл при критической ошибке базы
                }
            }
        }


        // =================================================================
        // 🛠 ОБРАБОТЧИКИ РЕАЛТАЙМ-РЕПЛИКАЦИИ ПОЛЬЗОВАТЕЛЕЙ (НА ЛЕТУ)
        // =================================================================

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg) =>
            ExecuteSyncTask(async (pg) =>
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация нового пользователя {msg.User.Id} в Postgres...");
                const string sql = "INSERT INTO users (id, first_name, last_name, phone_number, email, password, created_at, updated_at, is_synced) " +
                                   "VALUES (@Id, @FirstName, @LastName, @PhoneNumber, @Email, @Password, @CreatedAt, @UpdatedAt, 1);";
                await pg.ExecuteAsync(sql, msg.User);
            }, "User Insert");

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg) =>
            ExecuteSyncTask(async (pg) =>
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация обновления пользователя {msg.User.Id} в Postgres...");
                const string sql = "UPDATE users SET first_name = @FirstName, last_name = @LastName, phone_number = @PhoneNumber, " +
                                   "email = @Email, password = @Password, updated_at = @UpdatedAt, is_synced = 1 WHERE id = @Id;";
                await pg.ExecuteAsync(sql, msg.User);
            }, "User Update");

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg) =>
            ExecuteSyncTask(async (pg) =>
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация удаления пользователя {msg.UserId} в Postgres...");
                await pg.ExecuteAsync("DELETE FROM users WHERE id = @Id", new { Id = msg.UserId });
            }, "User Delete");

        // =================================================================
        // 🛠 ОБРАБОТЧИКИ РЕАЛТАЙМ-РЕПЛИКАЦИИ СООБЩЕНИЙ (НА ЛЕТУ)
        // =================================================================

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg) =>
            ExecuteSyncTask(async (pg) =>
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация сообщения {msg.Message.Id} в центральный Postgres...");
                const string sql = "INSERT INTO messages (id, sender_id, receiver_id, text, media_type, file_path, is_read, created_at, updated_at, is_synced) " +
                                   "VALUES (@Id, @SenderId, @ReceiverId, @Text, @MediaType, @FilePath, @IsRead, @CreatedAt, @UpdatedAt, 1);";
                await pg.ExecuteAsync(sql, msg.Message);
            }, "Message Insert");

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg) =>
            ExecuteSyncTask(async (pg) =>
            {
                _logger.LogInfo($"[СИНХРОНИЗАТОР] Репликация статуса сообщения {msg.Message.Id} в Postgres...");
                const string sql = "UPDATE messages SET is_read = @IsRead, text = @Text, updated_at = @UpdatedAt, is_synced = 1 WHERE id = @Id;";
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
                    var pgConnection = _postgresBuilder.Connection;
                    if (pgConnection.State != System.Data.ConnectionState.Open)
                    {
                        await pgConnection.OpenAsync();
                    }
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
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
        }
    }
}