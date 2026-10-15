using HomeNet.Network;              
using HomeNet.Network.HomeNet.Network;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Network; 
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Repositories;
using HomeNetOrm.Sinc;


namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        public IDbEngine SqliteEngine { get; }
        public IApiClient ApiClient { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepository UserRepository { get; }
        public IMessageRepository MessageRepository { get; }
        public IFriendRepository FriendRepository { get; }
        public ISiberNetSyncCoordinator SyncCoordinator { get; }

        // Теперь здесь лежит наш чистый, универсальный слушатель из HomeNet.Network
        public SignalRNotificationListener NotificationListener { get; }

        public DataAccessModule(string sqliteConn, string apiBaseUrl, ILogger logger, IEventBus eventBus)
        {
            // 1. Инициализируем локальный движок
            SqliteEngine = new SqliteDbEngine(sqliteConn, logger);

            // 2. Инициализируем сетевой HttpClient
            ApiClient = new HttpApiClient(apiBaseUrl, logger);

            // 3. ФИКС: Скармливаем контейнеру новые абстрактные параметры
            DbContextContainer = new DbContextContainer(SqliteEngine, apiBaseUrl, eventBus, logger);

            UserRepository = new UserRepository(SqliteEngine, eventBus);
            MessageRepository = new MessageRepository(SqliteEngine, eventBus);
            FriendRepository = new FriendRepository(SqliteEngine, eventBus);

            // 4. Передаем ApiClient в синхронизаторы
            var userSync = new UserSynchronizer(SqliteEngine, ApiClient, DbContextContainer, eventBus, logger);
            var messageSync = new MessageSynchronizer(SqliteEngine, ApiClient, DbContextContainer, eventBus, logger);

            SyncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);

            // 5. ФИКС: Собираем типизированные сетевые обработчики по SOLID
            var networkHandlers = new INetworkHandler[]
            {
                new MessageNetworkHandler(eventBus),
                new UserNetworkHandler(eventBus)
            };

            // 6. ФИКС: Собираем универсальный листенер без жесткой привязки к СУБД
            NotificationListener = new SignalRNotificationListener(apiBaseUrl, networkHandlers, logger);
        }
    }

    public class CompositeSyncCoordinator : ISiberNetSyncCoordinator
    {
        private readonly ISiberNetSyncCoordinator _userSync;
        private readonly ISiberNetSyncCoordinator _messageSync;

        public CompositeSyncCoordinator(ISiberNetSyncCoordinator userSync, ISiberNetSyncCoordinator messageSync)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            _userSync = userSync;
            _messageSync = messageSync;
        }

        public async Task StartAsync()
        {
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}
