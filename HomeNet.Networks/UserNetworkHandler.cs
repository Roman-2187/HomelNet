using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetCore.Extensions;

namespace HomeNet.Network
{
    public class UserNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveUserSync";

        public UserNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        protected override void SubscribeToEvents()
        {
            // 1. ИСХОДЯЩИЕ: Слушаем запросы на синхронизацию и удаление от UserSynchronizer
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
        }

        // =========================================================================
        // НАПРАВЛЕНИЕ "НА СЕРВЕР": Отправка и удаление данных на бэкенде
        // =========================================================================
        private void OnUserPendingSync(ISiberNetSyncCoordinator.UserPendingSync @event)
        {
            var user = @event.User;
            FireAndForget(async () =>
            {
                Logger.LogInfo($"[UserNet] Отправляем профиль {user.Id} на сервер...");
                if (await ApiClient.PushUserAsync(user))
                {
                    EventBus.Publish(this, new ISiberNetSyncCoordinator.UserSyncedOnServer(user.Id));
                }
            }, $"Push User {user.Id}");
        }

        private void OnUserPendingDelete(ISiberNetSyncCoordinator.UserPendingDelete @event)
        {
            var userId = @event.UserId;
            FireAndForget(async () =>
            {
                Logger.LogWarning($"[UserNet] Запрос удаления пользователя {userId} на сервере...");
                if (await ApiClient.DeleteUserAsync(userId))
                {
                    EventBus.Publish(this, new ISiberNetSyncCoordinator.UserDeleteConfirmedByServer(userId));
                }
            }, $"Delete User {userId}");
        }

        // =========================================================================
        // НАПРАВЛЕНИЕ "С СЕРВЕРА": Прием данных профиля и команд удаления от бэкенда
        // =========================================================================
        // Дергается сетью при наступлении "ReceiveUserSync"
        public Task HandleIncomingUserAsync(UserEntity user)
        {
            if (user == null) return Task.CompletedTask;

            Logger.LogInfo($"[UserNet] Сервер прислал апдейт профиля {user.Id}. Отправляем в шину.");
            user.IsSynced = 1;

            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(user));
            return Task.CompletedTask;
        }

        // НАПРАВЛЕНИЕ "С СЕРВЕРА": Сервер прислал команду удаления юзера
        public Task HandleIncomingUserDeletedAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Сервер прислал команду удаления юзера {userId}. Передаем подтверждение в шину.");

            // ВМЕСТО ЕВЕНТА УДАЛЕНИЯ ШЛЕМ ЕВЕНТ ПОДТВЕРЖДЕНИЯ! 🎯
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserDeleteConfirmedByServer(userId));
            return Task.CompletedTask;
        }


        public override void Dispose()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
        }
    }
}
