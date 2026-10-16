using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using System;
using System.Threading.Tasks;

namespace HomeNetOrm.Sinc
{
    /// <summary>
    /// Базовый диспетчер синхронизации. Отвязан от СУБД и контейнеров! 🦾
    /// </summary>
    public abstract class BaseSynchronizer<TEntity> : ISiberNetSyncCoordinator, IDisposable
        where TEntity : class
    {
        protected readonly IApiClient ApiClient;
        protected readonly IEventBus EventBus;
        protected readonly ILogger Logger;

        private bool _isInitialSyncExecuted;

        protected BaseSynchronizer(
            IApiClient apiClient,
            IEventBus eventBus,
            ILogger logger)
        {
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        // Потомок сам решит, как делать пакетную сверку через свой репозиторий
        protected abstract Task ExecuteTableSyncInternalAsync(IApiClient api);

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            // Проверяем доступность сервера напрямую через сетевой контракт!
            if (!ApiClient.IsServerAvailable)
            {
                Logger.LogWarning($"[{GetType().Name}] Центральный сервер API недоступен. Пакетная сверка пропущена.");
                return;
            }

            try
            {
                Logger.LogInfo($"=== [{GetType().Name}] ЗАПУСК ПАКЕТНОЙ СВЕРКИ ЧЕРЕЗ API ===");

                await ExecuteTableSyncInternalAsync(ApiClient);

                Logger.LogInfo($"=== [{GetType().Name}] ПАКЕТНАЯ СВЕРКА ЧЕРЕЗ API ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Катастрофа пакетной синхронизации: {ex.Message}");
            }
        }

        // Запуск фоновых задач отправки "на лету"
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
