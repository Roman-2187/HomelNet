using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;

namespace HomeNetOrm.Sinc
{
    /// <summary>
    /// Базовый диспетчер синхронизации. Теперь реально отвязан от сети! 🦾
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

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            try
            {
                Logger.LogInfo($"=== [{GetType().Name}] ЗАПУСК ЛОКАЛЬНОЙ СВЕРКИ ===");

                await ExecuteTableSyncInternalAsync();

                Logger.LogInfo($"=== [{GetType().Name}] ЛОКАЛЬНАЯ СВЕРКА ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Ошибка при запуске сверки: {ex.Message}");
            }
        }

        public virtual void Dispose()
        {
            UnsubscribeEvents();
        }
    }
}
