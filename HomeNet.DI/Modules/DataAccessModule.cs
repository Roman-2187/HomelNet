using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Network.SignalRListeners;
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
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepo UserRepository { get; }
        public IMessageRepo MessageRepository { get; }
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }
        public Guid CurrentDeviceId { get; private set; }
        public SyncEngine OrmEngine { get; }

        public UserSignalRListener UserListener { get; }
        public MessageSignalRListener MessageListener { get; }

        // 🔥 ДОБАВЛЯЕМ СВОЙСТВА ДЛЯ API-КЛИЕНТОВ
        public IUserApiClient UserApiClient { get; }
        public IMessageApiClient MessageApiClient { get; }

        public UserSyncService UserSyncService { get; }
        public MessageSyncService MessageSyncService { get; }

        public DataAccessModule(
            string sqliteConn, string apiBaseUrl,
            IEnumerable<TableSchema> appSchemas,
            ICastomLogger logger, ILogQueueManager logQueue,
            IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

            // 1. Движок и репозитории
            DbEngine = new SiberNetDbEngine(DbEngineType.SQLiteSQLClient, sqliteConn, appSchemas, logger);
            DbEngine.InitializeRepositories(eventBus);
            UserRepository = DbEngine.Repositories.GetRepository<IUserRepo>();
            MessageRepository = DbEngine.Repositories.GetRepository<IMessageRepo>();
            DeviceSettingsRepository = DbEngine.Repositories.GetRepository<ClientDeviceSettingsRepository>();

            // 2. HttpClient
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            // 3. API-клиенты (и сохраняем их в свойства!)
            UserApiClient = new UserApiClient(httpClient, logger, eventBus);
            MessageApiClient = new MessageApiClient(httpClient, logger, eventBus);

            // 4. Контейнер хаба
            DbContextContainer = new DbContextContainer(DbEngine, apiBaseUrl, eventBus, logger);

            // 🔥 5. Создаем SyncService
            UserSyncService = new UserSyncService(UserRepository, UserApiClient, eventBus, logger);
            MessageSyncService = new MessageSyncService(MessageRepository, MessageApiClient, eventBus, logger);

            // 6. Запускаем стартовую синхронизацию
            var syncCoordinator = new CompositeSyncCoordinator(UserSyncService, MessageSyncService);
            OrmEngine = new SyncEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            // 7. SignalR-листенеры
            UserListener = new UserSignalRListener(apiBaseUrl, logger, eventBus);
            MessageListener = new MessageSignalRListener(apiBaseUrl, logger, eventBus);
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