using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Local;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class LocalRepositoryFactory(
        Func<DbConnection> connectionFactory,
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger,
        IEventBus eventBus) : BaseRepositoryFactory(connectionFactory, schemas, translator, logger, eventBus)
    {

        // 🔥 Реализуем фабричные методы (с шиной)
        internal override IUserRepo CreateUserRepository(IUserSqlGenerator generator)
            => new UserRepository(ConnectionFactory, generator, Logger, eventBus);

        internal override IMessageRepo CreateMessageRepository(IMessageSqlGenerator generator)
            => new MessageRepository(ConnectionFactory, generator, Logger, eventBus);

        // 🔥 Добавляем специфичный для клиента репозиторий
        protected override void RegisterSpecificRepositories()
        {
            var deviceSettingsGen = new DeviceSettingsSqlGenerator(Schemas, Translator, Logger);
            Repositories[typeof(ClientDeviceSettingsRepository)] = new ClientDeviceSettingsRepository(ConnectionFactory, deviceSettingsGen);
        }
    }
}