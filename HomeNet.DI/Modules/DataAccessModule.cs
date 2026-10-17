using HomeNet.Network;
using HomeNet.Network.HomeNet.Network;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Network;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Models;
using HomeNetOrm.Sinc;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        public SqliteDbEngine SqliteEngine { get; }
        public IApiClient ApiClient { get; }
        public IDbContextContainer DbContextContainer { get; }

        // Чистые интерфейсы репозиториев наружу для Avalonia
        public IUserRepository UserRepository { get; }
        public IMessageRepository MessageRepository { get; }

        // Наш главный собранный движок ОРМ наружу
        public SiberNetEngine OrmEngine { get; }

        // Сетевой слушатель SignalR
        public SignalRNotificationListener NotificationListener { get; }

        // 🎯 КОНСТРУКТОР ТЕПЕРЬ ТРЕТЬИМ ПАРАМЕТРОМ ПРИНИМАЕТ СХЕМЫ ТАБЛИЦ
        public DataAccessModule(string sqliteConn, string apiBaseUrl, IEnumerable<TableSchema> appSchemas, ILogger logger, ILogQueueManager logQueue, IEventBus eventBus)
        {
            // 1. Инициализируем локальный движок SQLite и скармливаем ему схемы! 🦾
            SqliteEngine = new SqliteDbEngine(sqliteConn, appSchemas, logger);

            // 2. Инициализируем сетевой HttpClient
            ApiClient = new HttpApiClient(apiBaseUrl, logger);

            // 3. Контейнер занимается только инициализацией баз на старте
            DbContextContainer = new DbContextContainer(SqliteEngine, apiBaseUrl, eventBus, logger);

            // 4. 🔥 ЧИСТОТА: Движок сам создаёт репозитории, скрывая internal-генераторы внутри себя!
            UserRepository = SqliteEngine.CreateUserRepository(eventBus);
            MessageRepository = SqliteEngine.CreateMessageRepository(eventBus);

            // 5. 🔥 ФИКС: Синхронизаторы теперь питаются чистыми репозиториями, без движков и контейнеров!
            var userSync = new UserSynchronizer(UserRepository, ApiClient, eventBus, logger);
            var messageSync = new MessageSynchronizer(MessageRepository, ApiClient, eventBus, logger);

            var syncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);

            // 6. 🔥 ФИКС: Закрываем всё это добро внутрь нашего единого SiberNetEngine движка ОРМ
            OrmEngine = new SiberNetEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            var networkHandlers = new INetworkHandler[]
        {
    new MessageNetworkHandler(eventBus),
    new UserNetworkHandler(eventBus),
    new ServerLogNetworkHandler(eventBus) // 🎯 Вернули автобус, компилятор счастлив!
        };


            NotificationListener = new SignalRNotificationListener(apiBaseUrl, networkHandlers, logger);
        }
    }

    /// <summary>
    /// Координатор последовательного или параллельного запуска табличных синхронизаторов
    /// </summary>
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
            // Запускаем пакетную сверку по обеим таблицам параллельно
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}
