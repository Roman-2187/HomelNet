using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Repositories;

namespace HomeNetOrm.Sinc
{
    public class MessageSynchronizer(IMessageRepository messageRepository, IEventBus eventBus, ILogger logger) : BaseSynchronizer<MessageEntity>(eventBus, logger)
    {
        private readonly IMessageRepository _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageInserted>(OnMessageInserted);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageUpdated>(OnMessageUpdated);
            EventBus.Subscribe<ISiberNetSyncCoordinator.MessageDeleted>(OnMessageDeleted);
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
                Logger.LogInfo($"[{GetType().Name}] Инициация синхронизации СМС...");

                var syncSupport = _messageRepository as ISyncSupport<MessageEntity>;
                if (syncSupport == null) return;

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Просим сеть скачать свежие сообщения
                // ============================================================================
                Logger.LogInfo($"[{GetType().Name}] Запрашиваем у автобуса новые сообщения с бэкенда...");

                EventBus.Publish(this, new ISiberNetSyncCoordinator.MessagePullRequested());

                // ============================================================================
                // 📤 НАПРАВЛЕНИЕ PUSH: Локальные оффлайн-хвосты СМС на отправку
                // ============================================================================
                var pendingSyncList = await syncSupport.GetUnsyncedRecordsAsync();
                var pendingList = pendingSyncList.ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных СМС. Пушим в автобус.");
                    foreach (var message in pendingList)
                    {
                        EventBus.Publish(this, new ISiberNetSyncCoordinator.MessagePendingSync(message));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при синхронизации СМС: {ex.Message}");
            }
        }

        private async void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null) return;

            // Если прилетело из сети готовое сообщение (IsSynced == 1), просто накатываем его в SQLite
            if (msg.Message.IsSynced == 1)
            {
                var syncSupport = _messageRepository as ISyncSupport<MessageEntity>;
                if (syncSupport != null)
                {
                    // === СТАЛО (Добавляем EventBus и Logger) 🎯 ===
                    await syncSupport.MergePulledRecordsAsync(new[] { msg.Message }, EventBus, Logger);
                }
                return;
            }

            // Локальное оффлайн-сообщение
            EventBus.Publish(this, new ISiberNetSyncCoordinator.MessagePendingSync(msg.Message));
        }

        private async void OnMessageSyncedOnServer(ISiberNetSyncCoordinator.MessageSyncedOnServer msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[MessageSync] Сервер подтвердил СМС '{msg.MessageId}'. Обновляем статус в SQLite.");
            // Приводим репозиторий сообщений к интерфейсу поддержки синхронизации ОРМ 🎯
            var syncSupport = _messageRepository as ISyncSupport<MessageEntity>;

            if (syncSupport != null)
            {
                // Теперь экстеншен отработает идеально и без лишних параметров типа!
                await syncSupport.MarkAsSyncedAsync(msg.MessageId);
            }
        }

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg) { }
        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg) { }
    }
}
