using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetCore.Interfaces;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Network
{
    public class UserNetworkHandler : BaseNetworkHandler
    {
        public override string ServerMethodName => "ReceiveUserSync";

        public UserNetworkHandler(IApiClient apiClient, IEventBus eventBus, ILogger logger)
            : base(apiClient, eventBus, logger) { }

        // Внутренние реализации защищённых фактов IUserRepo для сквозного мёржа
        private record UserInsertedFact(UserEntity User, string? Message) : IUserRepo.IInserted;
        private record UserDeletedFact(Guid UserId) : IUserRepo.IDeleted;

        protected override void SubscribeToEvents()
        {
            // 🔥 Переподключаемся на лаконичные рекорды-братья синхронизации пользователей
            EventBus.Subscribe<IUserSyncActions.Pending>(OnUserPendingSync);
            EventBus.Subscribe<IUserSyncActions.DeletePending>(OnUserPendingDelete);
            EventBus.Subscribe<IUserSyncActions.PullRequested>(OnUserPullRequested);
        }

        private void OnUserPendingSync(IUserSyncActions.Pending @event)
        {
            FireAndForget(() => PushUserInternalAsync(@event.User), $"Push User {@event.User.Id}");
        }

        private async Task PushUserInternalAsync(UserEntity user)
        {
            Logger.LogInfo($"[UserNet] Отправляем профиль {user.Id} на сервер...");
            if (await ApiClient.PushUserAsync(user))
            {
                // 🔥 Кидаем укрощённое подтверждение синхронизатору
                EventBus.Publish(this, new IUserSyncActions.Confirmed(user.Id));
            }
        }

        private void OnUserPendingDelete(IUserSyncActions.DeletePending @event)
        {
            FireAndForget(() => DeleteUserInternalAsync(@event.UserId), $"Delete User {@event.UserId}");
        }

        private async Task DeleteUserInternalAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Запрос удаления пользователя {userId} на сервере...");
            await ApiClient.DeleteUserAsync(userId);
        }

        private void OnUserPullRequested(IUserSyncActions.PullRequested @event)
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

            Logger.LogInfo($"[UserNet] Сервер прислал апдейт профиля {user.Id}. Отправляем защищённый факт в шину.");
            user.IsSynced = 1;

            // 🔥 Выстреливаем фабрикой защищённого факта ОРМ
            EventBus.Publish(this, new UserInsertedFact(user, "Скачан из сети"));
            return Task.CompletedTask;
        }

        // 🎯 ЕДИНСТВЕННЫЙ ИСТОЧНИК ПРАВДЫ ДЛЯ УДАЛЕНИЯ:
        public Task HandleIncomingUserDeletedAsync(Guid userId)
        {
            Logger.LogWarning($"[UserNet] Сервер прислал команду удаления юзера {userId}. Передаем подтверждение удаления.");

            // 🔥 Публикуем укрощённый факт-ответ сервера для координатора
            EventBus.Publish(this, new IUserSyncActions.DeleteConfirmed(userId));
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            EventBus.Unsubscribe<IUserSyncActions.Pending>(OnUserPendingSync);
            EventBus.Unsubscribe<IUserSyncActions.DeletePending>(OnUserPendingDelete);
            EventBus.Unsubscribe<IUserSyncActions.PullRequested>(OnUserPullRequested);
        }
    }
}
