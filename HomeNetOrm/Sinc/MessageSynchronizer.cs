using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;

namespace HomeNetOrm.Sinc
{
    public class MessageSynchronizer : BaseSynchronizer<MessageEntity>
    {
        private readonly IMessageRepository _messageRepository;

        public MessageSynchronizer(
            IMessageRepository messageRepository,
            IEventBus eventBus,
            ILogger logger)
            : base(eventBus, logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        protected override void SubscribeEvents()
        {
            // Локальные события изменений
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);

            // Ответ от сетевого слоя об успешной отправке на сервер
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageSyncedOnServer>(OnMessageSyncedOnServer);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.MessageSyncedOnServer>(OnMessageSyncedOnServer);
        }

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Инициация пакетной сверки СМС через ОРМ...");

                var syncSupport = _messageRepository as HomeNetOrm.Interfaces.DbInfrastructure.ISyncSupport<MessageEntity>;
                if (syncSupport == null) return;

                var localConn = syncSupport.Connection;
                var localGen = syncSupport.SqlGenerator;

                // 🎯 ХАК: Вызываем твой рабочий SQLite генератор запросов для СМС!
                string sql = HomeNetOrm.DBProviders.Sqlite.SqliteSincExtensions.GenerateSelectUnsyncedSqlite(localGen);

                var unsyncedMessages = await localConn.QueryAsync<MessageEntity>(sql);
                var pendingList = unsyncedMessages.ToList();

                if (pendingList.Count == 0)
                {
                    Logger.LogInfo($"[{GetType().Name}] Сверка завершена. Оффлайн-хвостов СМС не обнаружено.");
                    return;
                }

                Logger.LogWarning($"[{GetType().Name}] ОРМ обнаружила {pendingList.Count} неотправленных СМС. Инициируем веерную отправку в сеть.");

                foreach (var message in pendingList)
                {
                    EventBus.Publish(this, new ISiberNetSyncCoordinator.MessagePendingSync(message));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при инициации сверки СМС: {ex.Message}");
            }
        }



        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            string preview = msg.Message.Text != null && msg.Message.Text.Length > 20
                ? msg.Message.Text.Substring(0, 20) + "..."
                : msg.Message.Text ?? string.Empty;

            Logger.LogInfo($"[MessageSync] Локальный пуш СМС от ID '{msg.Message.SenderId}'. Публикуем запрос на синхронизацию.");

            // Просто уведомляем шину, что сообщение ждет отправки в сеть
            EventBus.Publish(this, new ISiberNetSyncCoordinator.MessagePendingSync(msg.Message));
        }

        private async void OnMessageSyncedOnServer(ISiberNetSyncCoordinator.MessageSyncedOnServer msg)
        {
            if (msg == null) return;

            Logger.LogInfo($"[MessageSync] Сервер подтвердил СМС '{msg.MessageId}'. Обновляем статус в SQLite.");

            // Чистое обновление базы на основе подтверждения из шины
            await _messageRepository.MarkAsSyncedAsync(msg.MessageId);
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
