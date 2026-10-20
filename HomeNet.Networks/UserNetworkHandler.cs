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
            // 1. ИСХОДЯЩИЕ (PUSH): Слушаем запросы на отправку и удаление от UserSynchronizer
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingDelete>(OnUserPendingDelete);

            // 2. ВХОДЯЩИЕ (PULL): Слушаем, когда синхронизатор просит скачать новые профили
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPullRequested>(OnUserPullRequested);
        }

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

        // 🔥 НАША НОВАЯ ЛОГИКА PULL:
        private void OnUserPullRequested(ISiberNetSyncCoordinator.UserPullRequested @event)
        {
            FireAndForget(async () =>
            {
                Logger.LogInfo("[UserNet] Сеть перехватила запрос PULL. Просим бэкенд выдать новых пользователей...");

                Guid currentDeviceId = Guid.NewGuid(); // Твой сохраненный паспорт девайса
                DateTimeOffset lastSyncedAt = DateTimeOffset.MinValue; // Твой временной чекпоинт

                var freshUsers = await ApiClient.FetchFreshUsersAsync(currentDeviceId, lastSyncedAt);

                foreach (var user in freshUsers)
                {
                    // Метод проставит IsSynced = 1 и выбросит UserInserted в автобус!
                    await HandleIncomingUserAsync(user);
                }
            }, "Pull Users From Server");
        }

        public Task HandleIncomingUserAsync(UserEntity user)
        {
            if (user == null) return Task.CompletedTask;

            Logger.LogInfo($"[UserNet] Сервер прислал апдейт профиля {user.Id}. Отправляем в шину.");
            user.IsSynced = 1;

            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(user));
            return Task.CompletedTask;
        }

        public Task HandleIncomingUserDeletedAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Сервер прислал команду удаления юзера {userId}. Передаем подтверждение в шину.");
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserDeleteConfirmedByServer(userId));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPullRequested>(OnUserPullRequested); // Чистим за собой
        }
    }
}
