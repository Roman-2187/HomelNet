using HomeNet.Network;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Local;
using HomeNetOrm.Sinc;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        // 🔥 Теперь здесь гордо трудится наша единая Царь-Машина!
        public SiberNetDbEngine DbEngine { get; }
        public IApiClient ApiClient { get; }
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }

        // Чистые интерфейсы репозиториев наружу для Avalonia
        public IUserRepository UserRepository { get; }
        public IMessageRepository MessageRepository { get; }

        // Конкретный репозиторий настроек девайса без лишних интерфейсов-пустышек
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }

        // Железный ID девайса из SQLite сессии
        public Guid CurrentDeviceId { get; private set; }

        // Наш главный синхро-движок ОРМ
        public SincEngine OrmEngine { get; }

        // Сетевой слушатель SignalR
        public SignalRNotificationListener NotificationListener { get; }

        public DataAccessModule(string sqliteConn, string apiBaseUrl, IEnumerable<TableSchema> appSchemas, ILogger logger, ILogQueueManager logQueue, IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

            // 🎯 1. Запускаем SiberNetDbEngine в режиме локального клиента (SQLite)
            DbEngine = new SiberNetDbEngine(DbEngineType.LocalClient, sqliteConn, appSchemas, logger);

            // 2. Сетевой курьер API
            ApiClient = new HttpApiClient(apiBaseUrl, logger);

            // 3. Контейнер хаба инициализации (принимает наш универсальный движок)
            DbContextContainer = new DbContextContainer(DbEngine, apiBaseUrl, eventBus, logger);

            // 4. Штампуем репозитории на автопилоте!
            UserRepository = DbEngine.CreateUserRepository(eventBus);
            MessageRepository = DbEngine.CreateMessageRepository(eventBus);
            DeviceSettingsRepository = DbEngine.CreateDeviceSettingsRepository();

            // 5. Инициализация слоев синхронизации
            var userSync = new UserSynchronizer(UserRepository, eventBus, logger);
            var messageSync = new MessageSynchronizer(MessageRepository, eventBus, logger);
            var syncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);

            // 6. Упаковываем репозитории в главный управляющий SincEngine
            OrmEngine = new SincEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            // 7. Сборка сетевых хандлеров
            var messageNetworkHandler = new MessageNetworkHandler(ApiClient, eventBus, logger);
            var userNetworkHandler = new UserNetworkHandler(ApiClient, eventBus, logger);

            // 8. Запуск SignalR листенера комьютаций
            NotificationListener = new SignalRNotificationListener(apiBaseUrl, messageNetworkHandler, userNetworkHandler, logger);
        }

        public async Task InitializeDeviceSessionAsync()
        {
            CurrentDeviceId = await DeviceSettingsRepository.GetOrCreateDeviceIdAsync();
        }
    }

    public class CompositeSyncCoordinator : ISiberNetSyncCoordinator
    {
        private readonly ISiberNetSyncCoordinator _userSync;
        private readonly ISiberNetSyncCoordinator _messageSync;

        public CompositeSyncCoordinator(ISiberNetSyncCoordinator userSync, ISiberNetSyncCoordinator messageSync)
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
