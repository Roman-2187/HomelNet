using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.Extensions;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Sinc;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    /// <summary>
    /// Автономный координатор синхронизации SiberNet ОРМ-слоя.
    /// Работает как чистый диспетчер через упакованные движки СУБД IDbEngine.
    /// </summary>
    public class SiberNetSyncCoordinator : ISiberNetSyncCoordinator, IDisposable
    {
        // 🔒 СЕМАФОР ДЛЯ ПОТОКОБЕЗОПАСНОСТИ: одновременно к базе пускаем только 1 таску
        private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

        // 🔥 ДВА УПАКОВАННЫХ АВТОНОМНЫХ ДВИЖКА БД
        private readonly IDbEngine _sqliteEngine;
        private readonly IDbEngine _postgresEngine;

        private readonly IDbContextContainer _contextContainer;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        private bool _isInitialSyncExecuted;

        public SiberNetSyncCoordinator(
            IDbEngine sqliteEngine,
            IDbEngine postgresEngine,
            IDbContextContainer contextContainer,
            IEventBus eventBus,
            ILogger logger)
        {
            _sqliteEngine = sqliteEngine ?? throw new ArgumentNullException(nameof(sqliteEngine));
            _postgresEngine = postgresEngine ?? throw new ArgumentNullException(nameof(postgresEngine));
            _contextContainer = contextContainer ?? throw new ArgumentNullException(nameof(contextContainer));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 🔌 Именованные подписки (Идеально для EventBusInspector)
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

            _logger.LogInfo("=== [СИНХРОНИЗАТОР] ЗАПУСК ПАКЕТНОЙ СВЕРКИ ЧЕРЕЗ МЕТОДЫ РАСШИРЕНИЯ ===");

            try
            {
                // Вытаскиваем живые подключения напрямую из наших движков
                var sqlite = _sqliteEngine.Connection;
                var pg = _postgresEngine.Connection;

                if (sqlite.State != System.Data.ConnectionState.Open) await sqlite.OpenAsync();
                if (pg.State != System.Data.ConnectionState.Open) await pg.OpenAsync();

                // 🚀 ПЕРЕХВАТЫВАЕМ ЦИФРЫ PULL С СЕРВЕРА (Генераторы достаем динамически из движков)
                int usersPulled = await sqlite.PullTableAsync(pg, _sqliteEngine.GetGenerator<UserEntity>(), _postgresEngine.GetGenerator<UserEntity>());
                int msgsPulled = await sqlite.PullTableAsync(pg, _sqliteEngine.GetGenerator<MessageEntity>(), _postgresEngine.GetGenerator<MessageEntity>());
                int friendsPulled = await sqlite.PullTableAsync(pg, _sqliteEngine.GetGenerator<FriendEntity>(), _postgresEngine.GetGenerator<FriendEntity>());

                if (usersPulled > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] Из PostgreSQL скачано новых пользователей: {usersPulled}");
                if (msgsPulled > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] Из PostgreSQL скачано новых сообщений: {msgsPulled}");
                if (friendsPulled > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] Из PostgreSQL скачано новых контактов: {friendsPulled}");

                // 🚀 ПЕРЕХВАТЫВАЕМ ЦИФРЫ PUSH НА СЕРВЕР
                int usersPushed = await sqlite.PushTableAsync(pg, _sqliteEngine.GetGenerator<UserEntity>(), _postgresEngine.GetGenerator<UserEntity>());
                int msgsPushed = await sqlite.PushTableAsync(pg, _sqliteEngine.GetGenerator<MessageEntity>(), _postgresEngine.GetGenerator<MessageEntity>());
                int friendsPushed = await sqlite.PushTableAsync(pg, _sqliteEngine.GetGenerator<FriendEntity>(), _postgresEngine.GetGenerator<FriendEntity>());

                if (usersPushed > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] На сервер PostgreSQL успешно выгружено оффлайн-пользователей: {usersPushed}");
                if (msgsPushed > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] На сервер PostgreSQL успешно выгружено оффлайн-сообщений: {msgsPushed}");
                if (friendsPushed > 0) _logger.LogInfo($"[СИНХРОНИЗАТОР] На сервер PostgreSQL успешно выгружено оффлайн-контактов: {friendsPushed}");

                _logger.LogInfo("=== [СИНХРОНИЗАТОР] ПАКЕТНАЯ СВЕРКА УСПЕШНО ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[СИНХРОНИЗАТОР] Катастрофа пакетной синхронизации: {ex.Message}");
            }
        }

        // =================================================================
        // 📡 ЯВНЫЕ ОБРАБОТЧИКИ РЕКОРДОВ С ЗАЩИТОЙ ОТ ПЕТЕЛЬ РЕПЛИКАЦИИ 🧼
        // =================================================================

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null) return;

            // 🔥 ЗАЩИТА: Если запись уже имеет статус 1 (пришла из сети / синхронизирована), тушим событие
            if (msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перехвачено событие: Пуш нового пользователя '{name}' (ID: {msg.User.Id}) в PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateInsert(), msg.User), $"User Insert ({name})");
        }

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            if (msg?.User == null) return;
            if (msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перехвачено событие: Синхронизация обновления профиля '{name}' в PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateUpdate(), msg.User), $"User Update ({name})");
        }

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            _logger.LogWarning($"[СИНХРОНИЗАТОР] Перехвачено событие: Удаление пользователя с ID '{msg.UserId}' из PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateDelete(), new { id = msg.UserId }), "User Delete");
        }

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null) return;

            // Блокируем петлю репликации
            if (msg.Message.IsSynced == 1) return;

            string preview = msg.Message.Text != null && msg.Message.Text.Length > 20
                ? msg.Message.Text.Substring(0, 20) + "..."
                : msg.Message.Text ?? string.Empty;

            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перехвачено событие: Пуш СМС от ID '{msg.Message.SenderId}' -> Текст: \"{preview}\" в PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<MessageEntity>();

            // 🔥 КРОСС-ПЛАТФОРМЕННЫЙ ХАК: Пересобираем параметры для Postgres.
            // Явно подсовываем true/false, чтобы Dapper сгенерировал корректный булев тип для сервера!
            var pgParameters = new
            {
                msg.Message.Id,
                msg.Message.SenderId,
                msg.Message.ReceiverId,
                msg.Message.Text,
                msg.Message.MediaType,
                IsRead = false,       // Новое сообщение всегда не прочитано (false)
                msg.Message.CreatedAt,
                IsSynced = true,      // На сервер оно улетает уже синхронизированным (true)
                msg.Message.UpdatedAt
            };

            // Передаем pgParameters вместо msg.Message
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateInsert(), pgParameters), "Message Insert");
        }


        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg)
        {
            if (msg?.Message == null) return;
            if (msg.Message.IsSynced == 1) return;

            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перехвачено событие: Обновление статуса СМС (ID: {msg.Message.Id}) в PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<MessageEntity>();
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateUpdate(), msg.Message), "Message Update");
        }

        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg)
        {
            _logger.LogWarning($"[СИНХРОНИЗАТОР] Перехвачено событие: Удаление СМС с ID '{msg.MessageId}' из PostgreSQL...");

            var pgGen = _postgresEngine.GetGenerator<MessageEntity>();
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(pgGen.GenerateDelete(), new { id = msg.MessageId }), "Message Delete");
        }

        private void ExecuteSyncTask(Func<DbConnection, Task> syncAction, string operationName)
        {
            if (!_contextContainer.IsPostgresAvailable) return;

            Task.Run(async () =>
            {
                // 1. Встаем в очередь и ждем, пока освободится подключение к Postgres
                await _dbLock.WaitAsync();

                try
                {
                  
                    var pgConnection = _postgresEngine.Connection;
                    if (pgConnection.State != System.Data.ConnectionState.Open) await pgConnection.OpenAsync();
                    // 2. Выполняем наш INSERT / UPDATE / DELETE в полной тишине и безопасности
                    await syncAction(pgConnection);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[СИНХРОНИЗАТОР] Сбой операции '{operationName}' при пуше в Postgres: {ex.Message}");
                }
                finally
                {
                    // 3. ОБЯЗАТЕЛЬНО ОТПУСКАЕМ семафор
                    _dbLock.Release();
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
            _dbLock.Dispose();
        }
    }
}