using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Extensions;
using HomeNetOrm.Interfaces;
using System.Data.Common;

namespace HomeNetOrm.Sync
{
    /// <summary>
    /// Автономный координатор синхронизации SiberNet ОРМ-слоя.
    /// Работает как чистый диспетчер через методы расширения, не влезая в кишки репозиториев.
    /// </summary>
    public class SiberNetSyncCoordinator : ISiberNetSyncCoordinator, IDisposable
    {

        // 🔒 СЕМАФОР ДЛЯ ПОТОКОБЕЗОПАСНОСТИ: одновременно к базе пускаем только 1 таску
        private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
        private readonly IDbConnectionBuilder _sqliteBuilder;
        private readonly IDbConnectionBuilder _postgresBuilder;
        private readonly IDbContextContainer _contextContainer;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        // Источники правды (Пары генераторов для каждой таблицы приложения)
        private readonly ISqlGenerator<UserEntity> _sqliteUserGen;
        private readonly ISqlGenerator<UserEntity> _pgUserGen;

        private readonly ISqlGenerator<MessageEntity> _sqliteMsgGen;
        private readonly ISqlGenerator<MessageEntity> _pgMsgGen;

        private readonly ISqlGenerator<FriendEntity> _sqliteFriendGen;
        private readonly ISqlGenerator<FriendEntity> _pgFriendGen;

        private bool _isInitialSyncExecuted;

        public SiberNetSyncCoordinator(
            IDbConnectionBuilder sqliteBuilder,
            IDbConnectionBuilder postgresBuilder,
            IDbContextContainer contextContainer,
            IEventBus eventBus,
            ILogger logger,
            // Затягиваем SQLite генераторы
            ISqlGenerator<UserEntity> sqliteUserGen,
            ISqlGenerator<MessageEntity> sqliteMsgGen,
            ISqlGenerator<FriendEntity> sqliteFriendGen,
            // Затягиваем Postgres генераторы
            ISqlGenerator<UserEntity> pgUserGen,
            ISqlGenerator<MessageEntity> pgMsgGen,
            ISqlGenerator<FriendEntity> pgFriendGen)
        {
            _sqliteBuilder = sqliteBuilder ?? throw new ArgumentNullException(nameof(sqliteBuilder));
            _postgresBuilder = postgresBuilder ?? throw new ArgumentNullException(nameof(postgresBuilder));
            _contextContainer = contextContainer ?? throw new ArgumentNullException(nameof(contextContainer));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _sqliteUserGen = sqliteUserGen ?? throw new ArgumentNullException(nameof(sqliteUserGen));
            _pgUserGen = pgUserGen ?? throw new ArgumentNullException(nameof(pgUserGen));

            _sqliteMsgGen = sqliteMsgGen ?? throw new ArgumentNullException(nameof(sqliteMsgGen));
            _pgMsgGen = pgMsgGen ?? throw new ArgumentNullException(nameof(pgMsgGen));

            _sqliteFriendGen = sqliteFriendGen ?? throw new ArgumentNullException(nameof(sqliteFriendGen));
            _pgFriendGen = pgFriendGen ?? throw new ArgumentNullException(nameof(pgFriendGen));

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
                var sqlite = _sqliteBuilder.Connection;
                var pg = _postgresBuilder.Connection;

                if (sqlite.State != System.Data.ConnectionState.Open) await sqlite.OpenAsync();
                if (pg.State != System.Data.ConnectionState.Open) await pg.OpenAsync();

                // 🔥 ШАГ 1: Потоковый PULL для всех трех таблиц в одну строчку!
                await sqlite.PullTableAsync(pg, _sqliteUserGen, _pgUserGen);
                await sqlite.PullTableAsync(pg, _sqliteMsgGen, _pgMsgGen);
                await sqlite.PullTableAsync(pg, _sqliteFriendGen, _pgFriendGen);

                // 🔥 ШАГ 2: Потоковый PUSH для всех трех таблиц в одну строчку!
                await sqlite.PushTableAsync(pg, _sqliteUserGen, _pgUserGen);
                await sqlite.PushTableAsync(pg, _sqliteMsgGen, _pgMsgGen);
                await sqlite.PushTableAsync(pg, _sqliteFriendGen, _pgFriendGen);

                _logger.LogInfo("=== [СИНХРОНИЗАТОР] ПАКЕТНАЯ СВЕРКА УСПЕШНО ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[СИНХРОНИЗАТОР] Катастрофа пакетной синхронизации: {ex.Message}");
            }
        }

        // =================================================================
        // 📡 ЯВНЫЕ ОБРАБОТЧИКИ РЕКОРДОВ ДЛЯ ИНСПЕКТОРА (НА ЛЕТУ В POSTGRES)
        // =================================================================

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgUserGen.GenerateInsert(), msg.User), "User Insert");

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgUserGen.GenerateUpdate(), msg.User), "User Update");

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgUserGen.GenerateDelete(), new { id = msg.UserId }), "User Delete");

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgMsgGen.GenerateInsert(), msg.Message), "Message Insert");

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgMsgGen.GenerateUpdate(), msg.Message), "Message Update");

        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg) =>
            ExecuteSyncTask(async (pg) => await pg.ExecuteAsync(_pgMsgGen.GenerateDelete(), new { id = msg.MessageId }), "Message Delete");

        private void ExecuteSyncTask(Func<DbConnection, Task> syncAction, string operationName)
        {
            if (!_contextContainer.IsPostgresAvailable) return;

            Task.Run(async () =>
            {
                // 1. Встаем в очередь и ждем, пока освободится подключение к Postgres
                await _dbLock.WaitAsync();

                try
                {
                    var pgConnection = _postgresBuilder.Connection;
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
                    // 3. ОБЯЗАТЕЛЬНО ОТПУСКАЕМ: открываем дверь для следующего юзера из очереди
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

            // Освобождаем семафор
            _dbLock.Dispose();
        }
    }
}
