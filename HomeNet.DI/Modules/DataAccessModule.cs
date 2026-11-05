using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network;
using HomeNet.Synchronization;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNet.Synchronization.Sinc;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        public SiberNetDbEngine DbEngine { get; }
        public IApiClient ApiClient { get; }
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }

        public IUserRepo UserRepository { get; }
        public IMessageRepo MessageRepository { get; }
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }

        public Guid CurrentDeviceId { get; private set; }
        public SyncEngine OrmEngine { get; }
        public SignalRNotificationListener NotificationListener { get; }

        public DataAccessModule(
            string sqliteConn, string apiBaseUrl,
            IEnumerable<TableSchema> appSchemas,
            ILogger logger, ILogQueueManager logQueue,
            IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

            // =========================================================================
            // 🔥 МАГИЯ ЗДЕСЬ: Разделяем шину на Открытую и Закрытую
            // =========================================================================
            if (eventBus is not IInternalEventBus internalBus)
            {
                throw new InvalidOperationException(
                    "[DataAccessModule] Критическая ошибка архитектуры: переданная шина не реализует IInternalEventBus. " +
                    "Репозитории не смогут публиковать факты!");
            }

            // 🎯 1. Запускаем SiberNetDbEngine в режиме локального клиента (SQLite)
            DbEngine = new SiberNetDbEngine(DbEngineType.SQLiteSQLClient, sqliteConn, appSchemas, logger);

            // 🔥 2. Инициализируем фабрику репозиториев (передаем закрытую шину)
            DbEngine.InitializeRepositories(internalBus);

            // 🔥 3. Достаем готовые репозитории из фабрики
            UserRepository = DbEngine.Repositories.GetRepository<IUserRepo>();
            MessageRepository = DbEngine.Repositories.GetRepository<IMessageRepo>();
            DeviceSettingsRepository = DbEngine.Repositories.GetRepository<ClientDeviceSettingsRepository>();

            // =========================================================================
            // 4. СЕТЕВОЙ КУРЬЕР API (Интерцептору нужна открытая шина)
            // =========================================================================
            var networkInterceptor = new NetworkExceptionInterceptor(eventBus)
            {
                InnerHandler = new HttpClientHandler()
            };

            var httpClient = new HttpClient(networkInterceptor)
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            ApiClient = new HttpApiClient(httpClient, logger);
            // =========================================================================

            // 5. Контейнер хаба инициализации
            DbContextContainer = new DbContextContainer(DbEngine, apiBaseUrl, internalBus, logger);

            // 6. Инициализация слоев синхронизации
            var userSync = new UserSynchronizer(UserRepository, internalBus, logger);
            var messageSync = new MessageSynchronizer(MessageRepository, internalBus, logger);
            var syncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);

            // 7. Упаковываем репозитории в главный управляющий SincEngine
            OrmEngine = new SyncEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            // 8. Сборка сетевых хандлеров
            var messageNetworkHandler = new MessageNetworkHandler(ApiClient, internalBus, logger);
            var userNetworkHandler = new UserNetworkHandler(ApiClient, internalBus, logger);

            // 9. Запуск SignalR листенера
            NotificationListener = new SignalRNotificationListener(apiBaseUrl, messageNetworkHandler, userNetworkHandler, logger, internalBus);
        }

        public async Task InitializeDeviceSessionAsync()
        {
            CurrentDeviceId = await DeviceSettingsRepository.GetOrCreateDeviceIdAsync();
        }
    }

    public class CompositeSyncCoordinator : ISyncCoordinator
    {
        private readonly ISyncCoordinator _userSync;
        private readonly ISyncCoordinator _messageSync;

        public CompositeSyncCoordinator(ISyncCoordinator userSync, ISyncCoordinator messageSync)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            _userSync = userSync ?? throw new ArgumentNullException(nameof(userSync));
            _messageSync = messageSync ?? throw new ArgumentNullException(nameof(messageSync));
        }

        public async Task StartAsync()
        {
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}