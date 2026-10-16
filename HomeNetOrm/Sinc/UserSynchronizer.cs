using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;

namespace HomeNetOrm.Sinc
{
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        private readonly IUserRepository _userRepository;

        public UserSynchronizer(
            IUserRepository userRepository, // Питаемся чистым репозиторием!
            IApiClient apiClient,
            IEventBus eventBus,
            ILogger logger)
            : base(apiClient, eventBus, logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

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

        protected override async Task ExecuteTableSyncInternalAsync(IApiClient api)
        {
            await Task.CompletedTask;
        }

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            Logger.LogInfo($"[UserSync] Перехвачен пуш профиля '{name}' (ID: {msg.User.Id}). Отправляем в API.");

            ExecuteSyncTask(async (apiClient) =>
            {
                bool success = await apiClient.PushUserAsync(msg.User);
                if (success)
                {
                    // Командуем репозиторию пометить запись как синхронизированную
                    await _userRepository.MarkAsSyncedAsync(msg.User.Id);
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
