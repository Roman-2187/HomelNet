using System.Data;
using System.Data.Common;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNetOrm.Repositories.Base
{
    public abstract class BaseRepository<TEntity> : IRepository where TEntity : class
    {
        protected readonly Func<DbConnection> ConnectionFactory;

        // Торчит наружу internal-ом: экстеншены репозитория его увидят, а бизнес-логика — нет!
        internal ISqlGenerator<TEntity> SqlGenerator { get; }
        public ICastomLogger Logger { get; }

        internal BaseRepository(Func<DbConnection> connectionFactory, ISqlGenerator<TEntity> sqlGenerator, ICastomLogger logger)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            SqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 🔥 УПРАВЛЕНИЕ ПУЛОМ СОЕДИНЕНИЙ: Чисто, асинхронно, без утечек
        public async Task<TResult> ExecuteAsync<TResult>(Func<DbConnection, Task<TResult>> query)
        {
            using var db = ConnectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            return await query(db);
        }

        public async Task ExecuteAsync(Func<DbConnection, Task> command)
        {
            using var db = ConnectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await command(db);
        }
    }
}
