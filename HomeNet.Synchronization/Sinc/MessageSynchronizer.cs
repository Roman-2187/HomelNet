using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions; // 🔥 ПОДКЛЮЧАЕМ НАШИ ОБВЕСЫ!
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNet.Synchronization.Sinc
{
    /// <summary>
    /// Автономный синхронизатор сообщений чата SiberNet.
    /// Переведен на твой ультра-короткий интерфейс IMessageRepo! 🧼🦾
    /// </summary>
    public class MessageSynchronizer(IMessageRepo messageRepository, IEventBus eventBus, ILogger logger)
        : BaseSynchronizer<MessageEntity>(eventBus, logger)
    {
        private readonly IMessageRepo _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<IMessageRepo.ISaved>(OnMessageInserted);
            EventBus.Subscribe<IMessageRepo.IUpdated>(OnMessageUpdated);
            EventBus.Subscribe<IMessageRepo.IDeleted>(OnMessageDeleted);
            EventBus.Subscribe<IMessageSyncActions.Confirmed>(OnMessageSyncedOnServer);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<IMessageRepo.ISaved>(OnMessageInserted);
            EventBus.Unsubscribe<IMessageRepo.IUpdated>(OnMessageUpdated);
            EventBus.Unsubscribe<IMessageRepo.IDeleted>(OnMessageDeleted);
            EventBus.Unsubscribe<IMessageSyncActions.Confirmed>(OnMessageSyncedOnServer);
        }

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Инициация синхронизации СМС...");

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Просим сеть скачать свежие сообщения
                // ============================================================================
                Logger.LogInfo($"[{GetType().Name}] Запрашиваем у автобуса новые сообщения с бэкенда...");
                EventBus.Publish(this, new IMessageSyncActions.PullRequested());

                // ============================================================================
                // 📤 НАПРАВЛЕНИЕ PUSH: Локальные оффлайн-хвосты СМС на отправку
                // ============================================================================
                // 🔥 ИСПРАВЛЕНО: Вызываем дженерик-экстеншен базового CRUD с указанием сущности!
                var pendingSyncList = await _messageRepository.GetAllAsync<MessageEntity>();
                var pendingList = pendingSyncList.Where(m => m.IsSynced == 0).ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных СМС. Пушим в автобус.");
                    foreach (var message in pendingList)
                    {
                        EventBus.Publish(this, new IMessageSyncActions.Pending(message));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при синхронизации СМС: {ex.Message}");
            }
        }

        private async void OnMessageInserted(IMessageRepo.ISaved msg)
        {
            if (msg?.Message == null) return;

            if (msg.Message.IsSynced == 1)
            {
                await _messageRepository.SaveMessageAsync(msg.Message);
                return;
            }

            EventBus.Publish(this, new IMessageSyncActions.Pending(msg.Message));
        }

        private async void OnMessageSyncedOnServer(IMessageSyncActions.Confirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[MessageSync] Сервер подтвердил СМС '{msg.MessageId}'. Обновляем статус в SQLite.");

            // 🔥 ИСПРАВЛЕНО: Вызываем наш специализированный, чистый экстеншен без кастов и коннекшенов!
            await _messageRepository.MarkMessageAsSyncedAsync(msg.MessageId);
        }

        private void OnMessageUpdated(IMessageRepo.IUpdated msg) { }
        private void OnMessageDeleted(IMessageRepo.IDeleted msg) { }
    }
}
