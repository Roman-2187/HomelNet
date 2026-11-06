using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNet.Network.Network.ApiClient; // 🔥
using HomeNetOrm.Repositories.Extensions;

namespace HomeNet.Synchronization.Sinc
{
    public class MessageSynchronizer : BaseSynchronizer<MessageEntity, IMessageApiClient>
    {
        private readonly IMessageRepo _messageRepository;

        public MessageSynchronizer(
            IMessageRepo messageRepository,
            IMessageApiClient apiClient,
            IEventBus eventBus,
            ICastomLogger logger)
            : base(eventBus, logger, apiClient)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<IMessageRepo.Saved>(OnMessageInserted);
            EventBus.Subscribe<IMessageRepo.Updated>(OnMessageUpdated);
            EventBus.Subscribe<IMessageRepo.Deleted>(OnMessageDeleted);
            EventBus.Subscribe<IMessageSyncActions.Confirmed>(OnMessageSyncedOnServer);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<IMessageRepo.Saved>(OnMessageInserted);
            EventBus.Unsubscribe<IMessageRepo.Updated>(OnMessageUpdated);
            EventBus.Unsubscribe<IMessageRepo.Deleted>(OnMessageDeleted);
            EventBus.Unsubscribe<IMessageSyncActions.Confirmed>(OnMessageSyncedOnServer);
        }

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Инициация синхронизации СМС...");

                // 📥 PULL: Скачиваем с сервера
                // (В реальном коде userId берется из сессии, пока ставим пустой)
                var freshMessages = await ApiClient.FetchFreshMessagesAsync(Guid.Empty, DateTimeOffset.MinValue);
                foreach (var message in freshMessages)
                {
                    message.IsSynced = 1;
                    EventBus.Publish(this, new IMessageRepo.SaveRequested(message));
                }

                // 📤 PUSH: Отправляем локальные оффлайн-хвосты
                var allMessages = await _messageRepository.GetAllAsync();
                var pendingList = allMessages.Where(m => m.IsSynced == 0).ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных СМС. Инициируем PUSH.");
                    foreach (var message in pendingList)
                    {
                        bool success = await ApiClient.PushMessageAsync(message);
                        if (success)
                        {
                            EventBus.Publish(this, new IMessageSyncActions.Confirmed(message.Id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при синхронизации СМС: {ex.Message}");
            }
        }

        private async void OnMessageInserted(IMessageRepo.Saved msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            bool success = await ApiClient.PushMessageAsync(msg.Message);
            if (success)
            {
                EventBus.Publish(this, new IMessageSyncActions.Confirmed(msg.Message.Id));
            }
        }

        private async void OnMessageUpdated(IMessageRepo.Updated msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            bool success = await ApiClient.PushMessageAsync(msg.Message);
            if (success)
            {
                EventBus.Publish(this, new IMessageSyncActions.Confirmed(msg.Message.Id));
            }
        }

        private async void OnMessageDeleted(IMessageRepo.Deleted msg)
        {
            // Логика удаления сообщения на сервере, если понадобится
            await Task.CompletedTask;
        }

        private async void OnMessageSyncedOnServer(IMessageSyncActions.Confirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[MessageSync] Сервер подтвердил СМС '{msg.MessageId}'. Обновляем статус в SQLite.");
            await _messageRepository.MarkMessageAsSyncedAsync(msg.MessageId);
        }
    }
}