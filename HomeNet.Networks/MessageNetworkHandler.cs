using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Extensions;
using HomeNetCore.Models.Entity;

namespace HomeNet.Network
{
    public class MessageNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveMessage";

        public MessageNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        protected override void SubscribeToEvents()
        {
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessagePullRequested>(OnMessagePullRequested);
        }

        private void OnMessagePendingSync(ISiberNetSyncCoordinator.MessagePendingSync @event)
        {
            FireAndForget(() => PushMessageInternalAsync(@event.Message), $"Push Message {@event.Message.Id}");
        }

        private async Task PushMessageInternalAsync(MessageEntity message)
        {
            Logger.LogInfo($"[MessageNet] Пушим сообщение {message.Id} на server...");
            bool success = await ApiClient.PushMessageAsync(message);
            if (success)
            {
                Logger.LogInfo($"[MessageNet] Сервер принял сообщение {message.Id}. Уведомляем синхронизатор.");
                EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageSyncedOnServer(message.Id));
            }
        }

        private void OnMessagePullRequested(ISiberNetSyncCoordinator.MessagePullRequested @event)
        {
            FireAndForget(PullMessagesInternalAsync, "Pull Messages From Server");
        }

        private async Task PullMessagesInternalAsync()
        {
            Logger.LogInfo("[MessageNet] Сеть перехватила запрос PULL. Запрашиваем посылку у PostgreSQL склада...");

            Guid currentDeviceId = Guid.NewGuid();
            DateTimeOffset lastSyncedAt = DateTimeOffset.MinValue;

            var freshMessages = await ApiClient.FetchFreshMessagesAsync(currentDeviceId, lastSyncedAt);

            Logger.LogInfo($"[MessageNet] Сервер выдал на блюдечке {freshMessages.Count} новых сообщений. Выгружаем в шину.");
            foreach (var message in freshMessages)
            {
                await HandleIncomingMessageAsync(message);
            }
        }

        public Task HandleIncomingMessageAsync(MessageEntity message)
        {
            if (message == null) return Task.CompletedTask;

            Logger.LogInfo($"[MessageNet] Сервер прислал новое сообщение {message.Id}. Кидаем в синхронизатор.");
            message.IsSynced = 1;

            EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessagePullRequested>(OnMessagePullRequested);
        }
    }
}
