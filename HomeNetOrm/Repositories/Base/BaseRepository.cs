using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    /// <summary>
    /// Декларативное ядро всех репозиториев ОРМ. 
    /// Берет на себя управление многопоточными фабриками соединений и контекстом синхронизации! 🦾
    /// </summary>
    public abstract class BaseRepository<TEntity> : ISyncSupport<TEntity> where TEntity : class

    {
        protected readonly Func<DbConnection> ConnectionFactory;
        internal readonly ISqlGenerator<TEntity> SqlGenerator;
        protected readonly ILogger Logger;

        // Реализуем интерфейс синхронизации на автомате
        ILogger ISyncSupport<TEntity>.Logger => Logger;
        ISqlGenerator<TEntity> ISyncSupport<TEntity>.SqlGenerator => SqlGenerator;

        // 💥 Магия полиморфизма: Для движка синхронизации мы на лету выдаем 
        // свежее, открытое изолированное соединение при обращении к свойству!
        DbConnection ISyncSupport<TEntity>.Connection
        {
            get
            {
                var conn = ConnectionFactory();
                if (conn.State != ConnectionState.Open) conn.Open();
                return conn;
            }
        }

        internal BaseRepository(Func<DbConnection> connectionFactory, ISqlGenerator<TEntity> sqlGenerator, ILogger logger)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            SqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Универсальный хэндлер выполнения асинхронных запросов в изолированном контексте.
        /// Инкапсулирует в себе using, открытие стейта и безопасныйDispose.
        /// </summary>
        protected async Task<TResult> ExecuteAsync<TResult>(Func<DbConnection, Task<TResult>> query)
        {
            using (var db = ConnectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                return await query(db);
            }
        }

        /// <summary>
        /// Универсальный хэндлер выполнения команд (INSERT/UPDATE/DELETE) без возвращаемого значения.
        /// </summary>
        protected async Task ExecuteAsync(Func<DbConnection, Task> command)
        {
            using (var db = ConnectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                await command(db);
            }
        }
    }
}
