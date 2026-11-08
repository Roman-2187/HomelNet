using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.UserZone; // Для IUserVm
using Microsoft.AspNetCore.SignalR.Client;

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

            EventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            EventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
        }

        private async void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User?.Id != null)
            {
                Logger.LogInfo($"[СЕТЬ] Поймали вход пользователя {msg.User.Id}. Поднимаем SignalR для {GetType().Name}...");
                await ConnectAsync(msg.User.Id);
            }
        }

        private async void OnUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User?.Id != null)
            {
                Logger.LogInfo($"[СЕТЬ] Поймали регистрацию пользователя {msg.User.Id}. Поднимаем SignalR для {GetType().Name}...");
                await ConnectAsync(msg.User.Id);
            }
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

            RegisterHandlers(Connection);

            Connection.On("ReceiveServerReady", () =>
            {
                Logger.LogInfo("[СЕТЬ] 🔥 Получен сигнал готовности от сервера!");
                EventBus.Publish(this, new ServerBecameReadyEvent());
                return Task.CompletedTask;
            });

            // 🔥 Оборачиваем запуск соединения в ExecuteSafeAsync
            // Если сервер недоступен, ошибка запишется в лог, но не упадет и не пойдет выше.
            await AutoExceptionHandler.ExecuteSafeAsync(Logger, async () =>
            {
                await Connection.StartAsync();
                return true; // Заглушка, так как метод требует возвращаемое значение
            });

            // Эта строчка выполнится в любом случае (даже если упало выше), но только если коннект живой
            if (Connection.State == HubConnectionState.Connected)
            {
                Logger.LogInfo($"[СЕТЬ] SignalR поднят для {GetType().Name}.");
            }
            else
            {
                Logger.LogWarning($"[СЕТЬ] SignalR не подключен для {GetType().Name}. Работаем в офлайн-режиме.");
            }
        }

        protected abstract void RegisterHandlers(HubConnection connection);

        public async ValueTask DisposeAsync()
        {
            EventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            EventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            if (Connection != null) await Connection.DisposeAsync();
        }
    }

    public record ServerBecameReadyEvent();
}