using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Remote;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class RemoteRepositoryFactory(
        Func<DbConnection> connectionFactory,
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger) : BaseRepositoryFactory(connectionFactory, schemas, translator, logger)
    {

        // 🔥 Реализуем фабричные методы (без шины)
        internal override IUserRepo CreateUserRepository(IUserSqlGenerator generator)
            => new UserRepository(ConnectionFactory, generator, Logger);

        internal override IMessageRepo CreateMessageRepository(IMessageSqlGenerator generator)
            => new MessageRepository(ConnectionFactory, generator, Logger);

        // 🔥 Добавляем специфичный для сервера репозиторий
        protected override void RegisterSpecificRepositories()
        {
            var deviceSyncGen = new DeviceSyncSqlGenerator(Schemas, Translator, Logger);
            Repositories[typeof(DeviceSyncRepository)] = new DeviceSyncRepository(ConnectionFactory, deviceSyncGen);
        }
    }
}