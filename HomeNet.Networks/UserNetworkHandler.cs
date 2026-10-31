using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNet.Network
{
    public class UserNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveUserSync";

        public UserNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        protected override void SubscribeToEvents()
        {
            EventBus.Subscribe<ISyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Subscribe<ISyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
            EventBus.Subscribe<ISyncCoordinator.UserPullRequested>(OnUserPullRequested);
        }

        private void OnUserPendingSync(ISyncCoordinator.UserPendingSync @event)
        {
            FireAndForget(() => PushUserInternalAsync(@event.User), $"Push User {@event.User.Id}");
        }

        private async Task PushUserInternalAsync(UserEntity user)
        {
            Logger.LogInfo($"[UserNet] Отправляем профиль {user.Id} на сервер...");
            if (await ApiClient.PushUserAsync(user))
            {
                EventBus.Publish(this, new ISyncCoordinator.UserSyncedOnServer(user.Id));
            }
        }

        private void OnUserPendingDelete(ISyncCoordinator.UserPendingDelete @event)
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

        private void OnUserPullRequested(ISyncCoordinator.UserPullRequested @event)
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

            EventBus.Publish(this, new ISyncCoordinator.UserInserted(user));
            return Task.CompletedTask;
        }

        // 🎯 ЕДИНСТВЕННЫЙ ИСТОЧНИК ПРАВДЫ ДЛЯ УДАЛЕНИЯ:
        // Сюда прилетает бродкаст сокета, и именно он один контролирует снос записи
        public Task HandleIncomingUserDeletedAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Сервер прислал команду удаления юзера {userId}. Передаем подтверждение в шину.");
            EventBus.Publish(this, new ISyncCoordinator.UserDeleteConfirmedByServer(userId));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<ISyncCoordinator.UserPendingSync>(OnUserPendingSync);
            EventBus.Unsubscribe<ISyncCoordinator.UserPendingDelete>(OnUserPendingDelete);
            EventBus.Unsubscribe<ISyncCoordinator.UserPullRequested>(OnUserPullRequested);
        }
    }
}
