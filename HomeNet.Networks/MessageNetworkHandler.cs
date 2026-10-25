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
            // 1. ИСХОДЯЩИЕ (PUSH): Слушаем, что синхронизатор просит отправить на сервер
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);

            // 2. ВХОДЯЩИЕ (PULL): Слушаем, когда синхронизатор просит затянуть новые СМС с сервера
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessagePullRequested>(OnMessagePullRequested);
        }

        private void OnMessagePendingSync(ISiberNetSyncCoordinator.MessagePendingSync @event)
        {
            var message = @event.Message;
            FireAndForget(async () =>
            {
                Logger.LogInfo($"[MessageNet] Пушим сообщение {message.Id} на server...");
                bool success = await ApiClient.PushMessageAsync(message);
                if (success)
                {
                    Logger.LogInfo($"[MessageNet] Сервер принял сообщение {message.Id}. Уведомляем синхронизатор.");
                    EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageSyncedOnServer(message.Id));
                }
            }, $"Push Message {message.Id}");
        }

        // 🔥 НАША НОВАЯ ЛОГИКА PULL:
        private void OnMessagePullRequested(ISiberNetSyncCoordinator.MessagePullRequested @event)
        {
            FireAndForget(async () =>
            {
                Logger.LogInfo("[MessageNet] Сеть перехватила запрос PULL. Запрашиваем посылку у PostgreSQL склада...");

                // TODO: Передаем в твой ApiClient данные текущей сессии девайса и чекпоинт времени
                // Эти данные можно брать из SessionManager или твоего стейта авторизации.
                Guid currentDeviceId = Guid.NewGuid(); // Временно, пока не прокинешь сюда SessionManager/контекст
                DateTimeOffset lastSyncedAt = DateTimeOffset.MinValue;

                var freshMessages = await ApiClient.FetchFreshMessagesAsync(currentDeviceId, lastSyncedAt);

                Logger.LogInfo($"[MessageNet] Сервер выдал на блюдечке {freshMessages.Count} новых сообщений. Выгружаем в шину.");
                foreach (var message in freshMessages)
                {
                    // Вызываем наш стандартный метод приема — он проставит IsSynced = 1
                    // и закинет сообщение в локальный SQLite репозиторий через OnMessageInserted!
                    await HandleIncomingMessageAsync(message);
                }
            }, "Pull Messages From Server");
        }

        public Task HandleIncomingMessageAsync(MessageEntity message)
        {
            if (message == null) return Task.CompletedTask;

            Logger.LogInfo($"[MessageNet] Сервер прислал новое сообщение {message.Id}. Кидаем в синхронизатор.");
            message.IsSynced = 1; // Пометка, что это готовые данные с сервера

            EventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessagePendingSync>(OnMessagePendingSync);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessagePullRequested>(OnMessagePullRequested); // Не забываем отписаться!
        }
    }
}
