using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;

namespace HomeNetOrm.Sinc
{
    /// <summary>
    /// Базовый диспетчер синхронизации. Теперь реально отвязан от сети и полностью безопасен! 🦾
    /// </summary>
    public abstract class BaseSynchronizer<TEntity> : ISiberNetSyncCoordinator, IDisposable
        where TEntity : class
    {
        protected readonly IEventBus EventBus;
        protected readonly ILogger Logger;

        private bool _isInitialSyncExecuted;

        protected BaseSynchronizer(IEventBus eventBus, ILogger logger)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
