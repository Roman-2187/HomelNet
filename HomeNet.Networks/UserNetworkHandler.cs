using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetCore.Extensions;
using HomeNetCore.Models.Entity;

namespace HomeNet.Network
{
    public class UserNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveUserSync";

        public UserNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        protected override void SubscribeToEvents()
        {
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserPullRequested>(OnUserPullRequested);
        }

        private void OnUserPendingSync(ISiberNetSyncCoordinator.UserPendingSync @event)
        {
            FireAndForget(() => PushUserInternalAsync(@event.User), $"Push User {@event.User.Id}");
        }

        private async Task PushUserInternalAsync(UserEntity user)
        {
            Logger.LogInfo($"[UserNet] Отправляем профиль {user.Id} на сервер...");
            if (await ApiClient.PushUserAsync(user))
            {
                EventBus.Publish(this, new ISiberNetSyncCoordinator.UserSyncedOnServer(user.Id));
            }
        }

        private void OnUserPendingDelete(ISiberNetSyncCoordinator.UserPendingDelete @event)
        {
            FireAndForget(() => DeleteUserInternalAsync(@event.UserId), $"Delete User {@event.UserId}");
        }

        private async Task DeleteUserInternalAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Запрос удаления пользователя {userId} на сервере...");

            // Просто пушим HTTP-запрос на удаление. 
            // Ждать ответа и пулять подтверждение отсюда НЕ НАДО — для этого ниже есть SignalR!
            await ApiClient.DeleteUserAsync(userId);
        }

        private void OnUserPullRequested(ISiberNetSyncCoordinator.UserPullRequested @event)
        {
            FireAndForget(PullUsersInternalAsync, "Pull Users From Server");
        }

        private async Task PullUsersInternalAsync()
        {
            Logger.LogInfo("[UserNet] Сеть перехватила запрос PULL. Просим бэкенд выдать новых пользователей...");

            Guid currentDeviceId = Guid.NewGuid();
            DateTimeOffset lastSyncedAt = DateTimeOffset.MinValue;

            var freshUsers = await ApiClient.FetchFreshUsersAsync(currentDeviceId, lastSyncedAt);

            foreach (var user in freshUsers)
            {
                await HandleIncomingUserAsync(user);
            }
        }

        public Task HandleIncomingUserAsync(UserEntity user)
        {
            if (user == null) return Task.CompletedTask;

            Logger.LogInfo($"[UserNet] Сервер прислал апдейт профиля {user.Id}. Отправляем в шину.");
            user.IsSynced = 1;

            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(user));
            return Task.CompletedTask;
        }

        // 🎯 ЕДИНСТВЕННЫЙ ИСТОЧНИК ПРАВДЫ ДЛЯ УДАЛЕНИЯ:
        // Сюда прилетает бродкаст сокета, и именно он один контролирует снос записи
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
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserPullRequested>(OnUserPullRequested);
        }
    }
}
