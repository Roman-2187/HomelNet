using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.Extensions;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Sinc;
using System.Data.Common;

namespace HomeNetOrm.Sinc
{
    public class MessageSynchronizer : BaseSynchronizer<MessageEntity>
    {
        public MessageSynchronizer(
            IDbEngine localEngine, IDbEngine remoteEngine,
            IDbContextContainer contextContainer, IEventBus eventBus, ILogger logger)
            : base(localEngine, remoteEngine, contextContainer, eventBus, logger) { }

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

        protected override async Task ExecuteTableSyncInternalAsync(DbConnection local, DbConnection remote)
        {
            int msgsPulled = await local.PullTableAsync(remote, LocalEngine.GetGenerator<MessageEntity>(), RemoteEngine.GetGenerator<MessageEntity>());
            int friendsPulled = await local.PullTableAsync(remote, LocalEngine.GetGenerator<FriendEntity>(), RemoteEngine.GetGenerator<FriendEntity>());

            if (msgsPulled > 0) Logger.LogInfo($"[MessageSync] Из сети скачано новых сообщений: {msgsPulled}");
            if (friendsPulled > 0) Logger.LogInfo($"[MessageSync] Из сети скачано новых контактов: {friendsPulled}");

            int msgsPushed = await local.PushTableAsync(remote, LocalEngine.GetGenerator<MessageEntity>(), RemoteEngine.GetGenerator<MessageEntity>());
            int friendsPushed = await local.PushTableAsync(remote, LocalEngine.GetGenerator<FriendEntity>(), RemoteEngine.GetGenerator<FriendEntity>());

            if (msgsPushed > 0) Logger.LogInfo($"[MessageSync] На сервер успешно выгружено оффлайн-сообщений: {msgsPushed}");
            if (friendsPushed > 0) Logger.LogInfo($"[MessageSync] На сервер успешно выгружено оффлайн-контактов: {friendsPushed}");
        }

        private void OnMessageInserted(ISiberNetSyncCoordinator.MessageInserted msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            string preview = msg.Message.Text != null && msg.Message.Text.Length > 20
                ? msg.Message.Text.Substring(0, 20) + "..."
                : msg.Message.Text ?? string.Empty;

            Logger.LogInfo($"[MessageSync] Перехвачен пуш СМС от ID '{msg.Message.SenderId}' -> Текст: \"{preview}\"");

            var remoteGen = RemoteEngine.GetGenerator<MessageEntity>();
            var pgParameters = new
            {
                msg.Message.Id,
                msg.Message.SenderId,
                msg.Message.ReceiverId,
                msg.Message.Text,
                msg.Message.MediaType,
                IsRead = false,
                msg.Message.CreatedAt,
                IsSynced = true,
                msg.Message.UpdatedAt
            };

            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateInsert(), pgParameters), "Message Insert");
        }

        private void OnMessageUpdated(ISiberNetSyncCoordinator.MessageUpdated msg)
        {
            if (msg?.Message == null || msg.Message.IsSynced == 1) return;

            var remoteGen = RemoteEngine.GetGenerator<MessageEntity>();
            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateUpdate(), msg.Message), "Message Update");
        }

        private void OnMessageDeleted(ISiberNetSyncCoordinator.MessageDeleted msg)
        {
            var remoteGen = RemoteEngine.GetGenerator<MessageEntity>();
            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateDelete(), new { id = msg.MessageId }), "Message Delete");
        }
    }
}
