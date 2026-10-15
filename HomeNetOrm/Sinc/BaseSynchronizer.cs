using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Builders;
using System.Data.Common;

namespace HomeNetOrm.Sinc
{
    public abstract class BaseSynchronizer<TEntity> : ISiberNetSyncCoordinator, IDisposable
        where TEntity : class
    {
        protected readonly IDbEngine LocalEngine;

        // Вместо IDbEngine RemoteEngine теперь используем наш сетевой контракт
        protected readonly IApiClient ApiClient;
        protected readonly IDbContextContainer ContextContainer;
        protected readonly IEventBus EventBus;
        protected readonly ILogger Logger;

        private bool _isInitialSyncExecuted;

        protected BaseSynchronizer(
            IDbEngine localEngine,
            IApiClient apiClient, // Меняем тут тип
            IDbContextContainer contextContainer,
            IEventBus eventBus,
            ILogger logger)
        {
            LocalEngine = localEngine ?? throw new ArgumentNullException(nameof(localEngine));
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            ContextContainer = contextContainer ?? throw new ArgumentNullException(nameof(contextContainer));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        // Метод сверки теперь принимает локальное подключение и клиент API
        protected abstract Task ExecuteTableSyncInternalAsync(DbConnection local, IApiClient api);

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            // Здесь проверяешь доступность веб-сервера через ContextContainer
            if (!ContextContainer.IsServerAvailable)
            {
                Logger.LogWarning($"[{GetType().Name}] Центральный сервер API недоступен. Пакетная сверка пропущена.");
                return;
            }

            try
            {
                Logger.LogInfo($"=== [{GetType().Name}] ЗАПУСК ПАКЕТНОЙ СВЕРКИ ЧЕРЕЗ API ===");

                var localConn = LocalEngine.Connection;
                if (localConn.State != System.Data.ConnectionState.Open) await localConn.OpenAsync();

                // Запускаем сверку, отдавая SQLite и HTTP клиент
                await ExecuteTableSyncInternalAsync(localConn, ApiClient);

                Logger.LogInfo($"=== [{GetType().Name}] ПАКЕТНАЯ СВЕРКА ЧЕРЕЗ API ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Катастрофа пакетной синхронизации: {ex.Message}");
            }
        }

        // Метод фоновой отправки одиночных записей (при перехвате событий из шины)
        protected void ExecuteSyncTask(Func<IApiClient, Task> syncAction, string operationName)
        {
            Task.Run(async () =>
            {
                try
                {
                    await syncAction(ApiClient);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[{GetType().Name}] Сбой операции '{operationName}' при отправке в API: {ex.Message}");
                }
            });
        }

        public virtual void Dispose()
        {
            UnsubscribeEvents();
        }
    }
}
