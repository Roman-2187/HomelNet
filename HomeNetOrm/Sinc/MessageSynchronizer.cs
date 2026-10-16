using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories; // Для IMessageRepository
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;

namespace HomeNetOrm.Sinc
{
    public class MessageSynchronizer : BaseSynchronizer<MessageEntity>
    {
        private readonly IMessageRepository _messageRepository;

        public MessageSynchronizer(
            IMessageRepository messageRepository, // Питаемся чистым репозиторием!
            IApiClient apiClient,
            IEventBus eventBus,
            ILogger logger)
            : base(apiClient, eventBus, logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
        }

        protected override async Task ExecuteTableSyncInternalAsync(IApiClient api)
        {
            // Пакетная сверка через HTTP GET-запросы встанет сюда позже
            await Task.CompletedTask;
        }

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            string preview = msg.Message.Text != null && msg.Message.Text.Length > 20
                ? msg.Message.Text.Substring(0, 20) + "..."
                : msg.Message.Text ?? string.Empty;

            Logger.LogInfo($"[MessageSync] Перехвачен пуш СМС от ID '{msg.Message.SenderId}' -> Текст: \"{preview}\"");

            ExecuteSyncTask(async (apiClient) =>
            {
                bool success = await apiClient.PushMessageAsync(msg.Message);
                if (success)
                {
                    // Чистота: командуем репозиторию обновить статус, никакого SQL!
                    await _messageRepository.MarkAsSyncedAsync(msg.Message.Id);
                }
            }, "Message Insert via API");
        }

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;
            Logger.LogInfo($"[MessageSync] Перехвачено обновление СМС '{msg.Message.Id}'");
        }

        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg)
        {
            Logger.LogWarning($"[MessageSync] Перехвачено удаление СМС с ID '{msg.MessageId}'");
        }
    }
}
