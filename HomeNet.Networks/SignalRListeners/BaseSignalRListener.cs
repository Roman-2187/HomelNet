using Microsoft.AspNetCore.SignalR.Client;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNet.Network.SignalRListeners
{
    public abstract class BaseSignalRListener : IAsyncDisposable
    {
        protected readonly string ApiBaseUrl;
        protected readonly ICastomLogger Logger;
        protected readonly IEventBus EventBus;
        protected HubConnection? Connection;

        protected BaseSignalRListener(string apiBaseUrl, ICastomLogger logger, IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;
            Logger = logger;
            EventBus = eventBus;
        }

        public async Task ConnectAsync(Guid? userId = null)
        {
            if (Connection != null) await Connection.DisposeAsync();

            string url = userId.HasValue
                ? $"{ApiBaseUrl}/chatHub?userId={userId.Value}"
                : $"{ApiBaseUrl}/chatHub";

            Connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            // 🔥 Даем наследнику зарегистрировать его обработчики
            RegisterHandlers(Connection);

            // Слушаем сигнал готовности сервера
            Connection.On("ReceiveServerReady", () =>
            {
                Logger.LogInfo("[СЕТЬ] 🔥 Получен сигнал готовности от сервера!");
                EventBus.Publish(this, new ServerBecameReadyEvent());
                return Task.CompletedTask;
            });

            await Connection.StartAsync();
            Logger.LogInfo($"[СЕТЬ] SignalR поднят для {GetType().Name}.");
        }

        // 🔥 Каждый наследник реализует это под свою зону
        protected abstract void RegisterHandlers(HubConnection connection);

        public async ValueTask DisposeAsync()
        {
            if (Connection != null) await Connection.DisposeAsync();
        }
    }

    public record ServerBecameReadyEvent();
}