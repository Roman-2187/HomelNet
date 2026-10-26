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
        public SincedSqliteDbEngine SqliteEngine { get; }
        public IApiClient ApiClient { get; }

        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }

        // Чистые интерфейсы репозиториев наружу для Avalonia
        public IUserRepository UserRepository { get; }
        public IMessageRepository MessageRepository { get; }

        // 🔥 ДОБАВИЛИ: Публичный репозиторий настроек девайса
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }

        // 🔥 ДОБАВИЛИ: Железный ID девайса, который мы вытащим из SQLite при старте сессии
        public Guid CurrentDeviceId { get; private set; }

        // Наш главный собранный движок ОРМ наружу
        public SincEngine OrmEngine { get; }

        // Сетевой слушатель SignalR
        public SignalRNotificationListener NotificationListener { get; }

        // 2. Внутри конструктора DataAccessModule первой строчкой сохрани прилетевший параметр:
        public DataAccessModule(string sqliteConn, string apiBaseUrl, IEnumerable<TableSchema> appSchemas, ILogger logger, ILogQueueManager logQueue, IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl; // 🔥 ФИКСИРУЕМ АДРЕС ДЛЯ ВНЕШНЕГО МИРА!
            // 1. Инициализируем локальный движок SQLite и скармливаем ему схемы! 🦾
            SqliteEngine = new SincedSqliteDbEngine(sqliteConn, appSchemas, logger);

            // 2. Инициализируем сетевой HttpClient (наш переводчик-курьер)
            ApiClient = new HttpApiClient(apiBaseUrl, logger);

            // 3. Контейнер занимается только инициализацией баз на старте
            DbContextContainer = new DbContextContainer(SqliteEngine, apiBaseUrl, eventBus, logger);

            // 4. Движок сам создаёт репозитории, скрывая internal-генераторы внутри себя!
            UserRepository = SqliteEngine.CreateUserRepository(eventBus);
            MessageRepository = SqliteEngine.CreateMessageRepository(eventBus);

            // 🔥 ШАГ 4.1: Чистое создание репозитория настроек СТРОГО через фабрику движка! 
            // Никаких генераторов в этом модуле нет, инкапсуляция соблюдена на все 100%.
            DeviceSettingsRepository = SqliteEngine.CreateDeviceSettingsRepository();

            // 5. 🔥 КРИСТАЛЬНАЯ ЧИСТОТА: Синхронизаторы теперь питаются ТОЛЬКО репозиторием, шиной и логгером. Сеть ушла наружу!
            var userSync = new UserSynchronizer(UserRepository, eventBus, logger);
            var messageSync = new MessageSynchronizer(MessageRepository, eventBus, logger);

            var syncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);

            // 6. Закрываем это добро внутрь нашего единого SincEngine движка ОРМ
            OrmEngine = new SincEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            // 7. 🔥 СБОРКА СЕТЕВОГО СЛОЯ: Создаем монолитные двусторонние хандлеры
            var messageNetworkHandler = new MessageNetworkHandler(ApiClient, eventBus, logger);
            var userNetworkHandler = new UserNetworkHandler(ApiClient, eventBus, logger);

            // 8. 🔥 НОВЫЙ КОММУТАТОР: Передаем хандлеры напрямую в листенер без всяких массивов и интерфейсов
            NotificationListener = new SignalRNotificationListener(apiBaseUrl, messageNetworkHandler, userNetworkHandler, logger);
        }

        /// <summary>
        /// 🔥 НОВЫЙ АСИНХРОННЫЙ МЕТОД: Инициализация сессии устройства
        /// Вызывать строго после того, как в бутстраппере отработает DbContextContainer.InitializeAsync()!
        /// </summary>
        public async Task InitializeDeviceSessionAsync()
        {
            // Репозиторий залезет в SQLite, заберет старый ID или сгенерирует новый без сырого SQL
            CurrentDeviceId = await DeviceSettingsRepository.GetOrCreateDeviceIdAsync();
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
            // Запускаем локальную сверку по обеим таблицам параллельно
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}
