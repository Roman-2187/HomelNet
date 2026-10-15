using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces; // Для IApiClient
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.Builders;
using System.Data.Common;

namespace HomeNetOrm.Sinc
{
    public class MessageSynchronizer : BaseSynchronizer<MessageEntity>
    {
        public MessageSynchronizer(
            IDbEngine localEngine, IApiClient apiClient, // Меняем тут тип
            IDbContextContainer contextContainer, IEventBus eventBus, ILogger logger)
            : base(localEngine, apiClient, contextContainer, eventBus, logger) { }

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

        protected override async Task ExecuteTableSyncInternalAsync(DbConnection local, IApiClient api)
        {
            // Пакетную сверку таблиц через HTTP запросы мы допишем чуть позже, 
            // когда сделаем GET-эндпоинты на сервере для скачивания истории.
            await Task.CompletedTask;
        }

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            string preview = msg.Message.Text != null && msg.Message.Text.Length > 20
                ? msg.Message.Text.Substring(0, 20) + "..."
                : msg.Message.Text ?? string.Empty;

            Logger.LogInfo($"[MessageSync] Перехвачен пуш СМС от ID '{msg.Message.SenderId}' -> Текст: \"{preview}\"");

            // Вместо генерации SQL-инсерта просто фигачим объект в API!
            ExecuteSyncTask(async (apiClient) =>
            {
                bool success = await apiClient.PushMessageAsync(msg.Message);
                if (success)
                {
                    // Проставляем IsSynced = 1 в локальной SQLite, чтобы больше не отправлять
                    using (var localConn = LocalEngine.Connection)
                    {
                        if (localConn.State != System.Data.ConnectionState.Open) await localConn.OpenAsync();
                        await localConn.ExecuteAsync("UPDATE messages SET is_synced = 1 WHERE id = @Id", new { msg.Message.Id });
                    }
                }
            }, "Message Insert via API");
        }

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            // Для прототипа апдейты можно пока пропустить или слать через тот же апи
            Logger.LogInfo($"[MessageSync] Перехвачено обновление СМС '{msg.Message.Id}'");
        }

        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg)
        {
            Logger.LogWarning($"[MessageSync] Перехвачено удаление СМС с ID '{msg.MessageId}'");
        }
    }
}
