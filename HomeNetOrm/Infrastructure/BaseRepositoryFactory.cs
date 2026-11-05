using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public abstract class BaseRepositoryFactory
    {
        protected readonly Func<DbConnection> ConnectionFactory;
        protected readonly IEnumerable<TableSchema> Schemas;
        protected readonly ISqlDialectTranslator Translator;
        protected readonly ILogger Logger;
        protected readonly IInternalEventBus? EventBus;

        protected readonly Dictionary<Type, object> Repositories = new();

        protected BaseRepositoryFactory(
            Func<DbConnection> connectionFactory,
            IEnumerable<TableSchema> schemas,
            ISqlDialectTranslator translator,
            ILogger logger,
            IInternalEventBus? eventBus)
        {
            ConnectionFactory = connectionFactory;
            Schemas = schemas;
            Translator = translator;
            Logger = logger;
            EventBus = eventBus;

            // 🔥 САМИ СОБИРАЕМ ОБЩИЕ РЕПОЗИТОРИИ
            InitializeCommonRepositories();

            // 🔥 А ТЕПЕРЬ НАСЛЕДНИК ДОБАВИТ СВОИ
            RegisterSpecificRepositories();
        }

        private void InitializeCommonRepositories()
        {
            var userGen = new UserSqlGenerator(Schemas, Translator, Logger);
            var msgGen = new MessageSqlGenerator(Schemas, Translator, Logger);

            // Создаем репозитории (какой конструктор вызывать — решает наследник через фабричный метод)
            Repositories[typeof(IUserRepo)] = CreateUserRepository(userGen);
            Repositories[typeof(IMessageRepo)] = CreateMessageRepository(msgGen);
        }

        // 🔥 Фабричные методы: наследники решают, с шиной или без
        internal abstract IUserRepo CreateUserRepository(IUserSqlGenerator generator);
        internal abstract IMessageRepo CreateMessageRepository(IMessageSqlGenerator generator);

        // 🔥 Абстрактный метод: наследники добавляют свои специфичные репозитории
        protected abstract void RegisterSpecificRepositories();

        public T GetRepository<T>() where T : class
        {
            if (Repositories.TryGetValue(typeof(T), out var repo))
                return (T)repo;

            throw new InvalidOperationException($"Репозиторий {typeof(T).Name} не зарегистрирован в фабрике.");
        }
    }
}