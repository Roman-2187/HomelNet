using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Sinc;
using HomeNetOrm.Repositories;
using HomeNetOrm.Sinc;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        public IDbEngine SqliteEngine { get; }
        public IDbEngine PostgresEngine { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepository UserRepository { get; }
        public IMessageRepository MessageRepository { get; }
        public IFriendRepository FriendRepository { get; }

        // Оставляем общий интерфейс координирования для AppCompositionRoot
        public ISiberNetSyncCoordinator SyncCoordinator { get; }

        public DataAccessModule(string sqliteConn, string postgresConn, ILogger logger, IEventBus eventBus)
        {
            // 1. Создаем реальные экземпляры наших движков
            SqliteEngine = new SqliteDbEngine(sqliteConn, logger);
            PostgresEngine = new PostgresDbEngine(postgresConn, logger);

            DbContextContainer = new DbContextContainer(SqliteEngine, PostgresEngine, eventBus, logger);

            UserRepository = new UserRepository(SqliteEngine, eventBus);
            MessageRepository = new MessageRepository(SqliteEngine, eventBus);
            FriendRepository = new FriendRepository(SqliteEngine, eventBus);

            // 2. ⚡ РЕФАКТОРИНГ СБОРКИ: Собираем композит под-синхронизаторов!
            // Передаем SqliteEngine как локальный источник, а PostgresEngine как удаленный сервер!
            var userSync = new UserSynchronizer(SqliteEngine, PostgresEngine, DbContextContainer, eventBus, logger);
            var messageSync = new MessageSynchronizer(SqliteEngine, PostgresEngine, DbContextContainer, eventBus, logger);

            // Обертка-композит, которая просто запустит StartAsync() для обоих наследников
            SyncCoordinator = new CompositeSyncCoordinator(userSync, messageSync);
        }
    }

    /// <summary>
    /// Легковесный композит-фасад, чтобы не ломать сигнатуры запуска в AppCompositionRoot
    /// </summary>
    public class CompositeSyncCoordinator : ISiberNetSyncCoordinator
    {
        private readonly ISiberNetSyncCoordinator _userSync;
        private readonly ISiberNetSyncCoordinator _messageSync;

        public CompositeSyncCoordinator(ISiberNetSyncCoordinator userSync, ISiberNetSyncCoordinator messageSync)
        {
            _userSync = userSync;
            _messageSync = messageSync;
        }

        public async Task StartAsync()
        {
            // Запускаем пакетные сверки параллельно!
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}
