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
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        public UserSynchronizer(
            IDbEngine localEngine, IApiClient apiClient, // Меняем тут тип
            IDbContextContainer contextContainer, IEventBus eventBus, ILogger logger)
            : base(localEngine, apiClient, contextContainer, eventBus, logger) { }

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);
        }

        protected override async Task ExecuteTableSyncInternalAsync(DbConnection local, IApiClient api)
        {
            // Пакетную сверку пользователей допишем вместе со сверкой сообщений
            await Task.CompletedTask;
        }

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            Logger.LogInfo($"[UserSync] Перехвачен пуш профиля '{name}' (ID: {msg.User.Id}). Отправляем в API.");

            // Вместо SQL Insert пушим профиль пользователя в сеть
            ExecuteSyncTask(async (apiClient) =>
            {
                bool success = await apiClient.PushUserAsync(msg.User);
                if (success)
                {
                    // Помечаем локально, что пользователь улетел в облако успешно
                    using (var localConn = LocalEngine.Connection)
                    {
                        if (localConn.State != System.Data.ConnectionState.Open) await localConn.OpenAsync();
                        await localConn.ExecuteAsync("UPDATE users SET is_synced = 1 WHERE id = @Id", new { msg.User.Id });
                    }
                }
            }, $"User Insert ({name}) via API");
        }

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;
            Logger.LogInfo($"[UserSync] Перехвачено обновление профиля '{msg.User.FirstName}'");
        }

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            Logger.LogWarning($"[UserSync] Перехвачено удаление пользователя с ID '{msg.UserId}'");
        }
    }
}
