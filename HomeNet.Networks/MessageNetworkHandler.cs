using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetCore.Extensions;

namespace HomeNet.Network
{
    public class MessageNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveMessage";

        public MessageNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        protected override void SubscribeToEvents()
        {
            // 1. ИСХОДЯЩИЕ: Слушаем, что синхронизатор просит отправить на сервер
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);
        }

        // =========================================================================
        // НАПРАВЛЕНИЕ "НА СЕРВЕР": Забираем C#, IApiClient превратит в JSON и отправит
        // =========================================================================
        private void OnMessagePendingSync(ISiberNetSyncCoordinator.MessagePendingSync @event)
        {
            var message = @event.Message;

            FireAndForget(async () =>
            {
                Logger.LogInfo($"[MessageNet] Пушим сообщение {message.Id} на сервер...");

                // ApiClient под капотом делает сериализацию в JSON и шлет HTTP POST
                bool success = await ApiClient.PushMessageAsync(message);

                if (success)
                {
                    Logger.LogInfo($"[MessageNet] Сервер принял сообщение {message.Id}. Уведомляем синхронизатор.");
                    EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageSyncedOnServer(message.Id));
                }
            }, $"Push Message {message.Id}");
        }

        // =========================================================================
        // НАПРАВЛЕНИЕ "С СЕРВЕРА": Сервер прислал JSON, мы получили C# объект и пуляем в шину
        // =========================================================================
        // Этот метод дергается твоим SignalR/сетевым движком при получении "ReceiveMessage"
        public Task HandleIncomingMessageAsync(MessageEntity message)
        {
            if (message == null) return Task.CompletedTask;

            Logger.LogInfo($"[MessageNet] Сервер прислал новое сообщение {message.Id}. Кидаем в синхронизатор.");
            message.IsSynced = 1; // Пометка, что это готовые данные с сервера

            // Швыряем в автобус — синхронизатор перехватит и запишет в базу
            EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);
        }
    }
}
