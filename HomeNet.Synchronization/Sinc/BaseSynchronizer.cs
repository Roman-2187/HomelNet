using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Synchronization.Interfaces.Sinc;

namespace HomeNet.Synchronization.Sinc
{
    /// <summary>
    /// Базовый диспетчер синхронизации. 
    /// Теперь он умеет работать с разными API-клиентами через дженерик.
    /// </summary>
    public abstract class BaseSynchronizer<TEntity, TApiClient> : ISyncCoordinator, IDisposable
        where TEntity : class
        where TApiClient : class
    {
        protected readonly IEventBus EventBus;
        protected readonly ICastomLogger Logger;
        protected readonly TApiClient ApiClient; // 🔥 Общий API-клиент для наследников

        private bool _isInitialSyncExecuted;

        protected BaseSynchronizer(IEventBus eventBus, ICastomLogger logger, TApiClient apiClient)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            SubscribeEvents();
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        // Потомок сам решит, как запустить локальную сверку и выкинуть неотправленное в шину
        protected abstract Task ExecuteTableSyncInternalAsync();

        // === ИСПРАВЛЕНИЕ: Единая безопасная точка вызова для любых фоновых задач синхронизации ===
        protected async Task SafeExecuteTableSyncAsync()
        {
            try
            {
                await ExecuteTableSyncInternalAsync();
            }
            catch (Exception ex)
            {
                // Ошибка перехвачена, записана в лог как [ERROR], но приложение НЕ падает
                Logger.LogError($"[{GetType().Name}] Критическая ошибка в фоновом потоке синхронизации: {ex.Message}");
            }
        }

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            Logger.LogInfo($"=== [{GetType().Name}] ЗАПУСК ЛОКАЛЬНОЙ СВЕРКИ ===");

            // Используем безопасный метод вместо прямого вызова
            await SafeExecuteTableSyncAsync();

            Logger.LogInfo($"=== [{GetType().Name}] ЛОКАЛЬНАЯ СВЕРКА ЗАВЕРШЕНА ===");
        }

        public virtual void Dispose()
        {
            UnsubscribeEvents();
        }
    }
}