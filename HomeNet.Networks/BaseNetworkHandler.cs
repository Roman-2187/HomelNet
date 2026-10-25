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

        // Имя метода, который мы слушаем ТАМ (на сервере)
        public abstract string ServerMethodName { get; }

        protected BaseNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
        {
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Метод для регистрации подписок потомка
            SubscribeToEvents();
        }

        // Этот метод каждый потомок переопределит, чтобы подписаться на свои команды из автобуса
        protected abstract void SubscribeToEvents();
        public abstract void Dispose();

        // Безопасный запуск сетевых задач, теперь ВООБЩЕ без фризов UI! 🦾
        protected void FireAndForget(Func<Task> action, string operationName)
        {
            // Мгновенно уходим в фоновый поток из пула, полностью освобождая поток Авалонии
            Task.Run(async () =>
            {
                try
                {
                    // Теперь проверка сети и её таймауты будут шуршать в фоне, не вешая интерфейс
                    if (!ApiClient.IsServerAvailable)
                    {
                        Logger.LogWarning($"[{GetType().Name}] Сервер недоступен. Операция '{operationName}' отменена.");
                        return;
                    }

                    await action();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[{GetType().Name}] Ошибка в '{operationName}': {ex.Message}");
                }
            });
        }
    }
}
