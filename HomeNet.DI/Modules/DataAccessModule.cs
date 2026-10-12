using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
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
        public ISiberNetSyncCoordinator SyncCoordinator { get; }

        public DataAccessModule(string sqliteConn, string postgresConn, ILogger logger, IEventBus eventBus)
        {
            SqliteEngine = new SqliteDbEngine(sqliteConn, logger);
            PostgresEngine = new PostgresDbEngine(postgresConn, logger);
            DbContextContainer = new DbContextContainer(SqliteEngine, PostgresEngine, eventBus, logger);

            UserRepository = new UserRepository(SqliteEngine, eventBus);
            MessageRepository = new MessageRepository(SqliteEngine, eventBus);
            FriendRepository = new FriendRepository(SqliteEngine, eventBus);

            SyncCoordinator = new SiberNetSyncCoordinator(
                sqliteEngine: SqliteEngine,
                postgresEngine: PostgresEngine,
                contextContainer: DbContextContainer,
                eventBus: eventBus,
                logger: logger
            );
        }
    }
}
