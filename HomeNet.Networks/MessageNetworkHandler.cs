using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Synchronization.Interfaces.Sinc; // 🔥 Подключаем укрощённых братьев-синхронизаторов
using HomeNetCore.Interfaces;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Network
{
    public class MessageNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveMessage";

        public MessageNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        // Приватная реализация защищённого факта репозитория, чтобы Авалония и синхронизатор подхватили СМС
        

        protected override void SubscribeToEvents()
        {
            // 🔥 Слушаем новые лаконичные рекорды-братья синхронизации
            EventBus.Subscribe<IMessageSyncActions.Pending>(OnMessagePendingSync);
            EventBus.Subscribe<IMessageSyncActions.PullRequested>(OnMessagePullRequested);
        }

        private void OnMessagePendingSync(IMessageSyncActions.Pending @event)
        {
            FireAndForget(() => PushMessageInternalAsync(@event.Message), $"Push Message {@event.Message.Id}");
        }

        private async Task PushMessageInternalAsync(MessageEntity message)
        {
            Logger.LogInfo($"[MessageNet] Пушим сообщение {message.Id} на server...");
            bool success = await ApiClient.PushMessageAsync(message);
            if (success)
            {
                Logger.LogInfo($"[MessageNet] Сервер принял сообщение {message.Id}. Уведомляем синхронизатор через Confirmed.");
                // 🔥 Публикуем укрощённое подтверждение для синхронизатора
                EventBus.Publish(this, new IMessageSyncActions.Confirmed(message.Id));
            }
        }

        private void OnMessagePullRequested(IMessageSyncActions.PullRequested @event)
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

            Logger.LogInfo($"[MessageNet] Сервер прислал новое сообщение {message.Id}. Кидаем в репозиторий через фабрику факта.");
            message.IsSynced = 1;

            // 🔥 Выстреливаем защищённым фактом репозитория! Синхронизатор запишет его, а UI отрисует без локов
            EventBus.Publish(this, new IMessageRepo.SaveRequested(message));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<IMessageSyncActions.Pending>(OnMessagePendingSync);
            EventBus.Unsubscribe<IMessageSyncActions.PullRequested>(OnMessagePullRequested);
        }
    }
}
