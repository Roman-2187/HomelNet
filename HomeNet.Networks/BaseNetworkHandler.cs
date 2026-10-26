using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Extensions;

namespace HomeNet.Network
{
    public abstract class BaseNetworkHandler : IDisposable
    {
        protected readonly IApiClient ApiClient;
        protected readonly IEventBus EventBus;
        protected readonly ILogger Logger;

        public abstract string ServerMethodName { get; }

        protected BaseNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
        {
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeToEvents();
        }

        protected abstract void SubscribeToEvents();
        public abstract void Dispose();

        // 🔥 ЧИСТЫЙ И БЕЗОПАСНЫЙ ЗАПУСК ЗАДАЧ БЕЗ СЛЕПЫХ БЛОКИРОВОК
        protected void FireAndForget(Func<Task> action, string operationName)
        {
            // Мгновенно уходим в фоновый поток
            Task.Run(async () =>
            {
                try
                {
                    // 🔥 УДАЛИЛИ БЛОКИРУЮЩИЙ И ЛОЖНЫЙ ТУМБЛЕР ApiClient.IsServerAvailable!
                    // Просто берем и выполняем сетевое действие. 
                    // Если бэкенд недоступен — оно само штатно упадет в catch ниже.
                    await action();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[{GetType().Name}] Ошибка выполнения операции '{operationName}': {ex.Message}");
                }
            });
        }
    }
}
