using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;

namespace HomeNet.Synchronization.Sinc
{
    public class UserSynchronizer : BaseSynchronizer<UserEntity, IUserApiClient>
    {
        private readonly IUserRepo _userRepository;

        public UserSynchronizer(
            IUserRepo userRepository,
            IUserApiClient apiClient,
            IEventBus eventBus,
            ICastomLogger logger)
            : base(eventBus, logger, apiClient) // Передаем клиент в базу
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        protected override void SubscribeEvents()
        {
            EventBus.Subscribe<IUserRepo.Inserted>(OnUserInserted);
            EventBus.Subscribe<IUserRepo.Updated>(OnUserUpdated);
            EventBus.Subscribe<IUserRepo.Deleted>(OnUserDeleted);
            EventBus.Subscribe<IUserSyncActions.Confirmed>(OnUserSyncedOnServer);
            EventBus.Subscribe<IUserSyncActions.DeleteConfirmed>(OnUserDeleteConfirmedByServer);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<IUserRepo.Inserted>(OnUserInserted);
            EventBus.Unsubscribe<IUserRepo.Updated>(OnUserUpdated);
            EventBus.Unsubscribe<IUserRepo.Deleted>(OnUserDeleted);
            EventBus.Unsubscribe<IUserSyncActions.Confirmed>(OnUserSyncedOnServer);
            EventBus.Unsubscribe<IUserSyncActions.DeleteConfirmed>(OnUserDeleteConfirmedByServer);
        }

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Запуск синхронизации профилей...");

                // 📥 PULL: Скачиваем с сервера
                var freshUsers = await ApiClient.FetchFreshUsersAsync(Guid.Empty, DateTimeOffset.MinValue);
                foreach (var user in freshUsers)
                {
                    user.IsSynced = 1;
                    // Отправляем команду в репозиторий на сохранение
                    EventBus.Publish(this, new IUserRepo.InsertRequested(user));
                }

                // 📤 PUSH: Отправляем локальные оффлайн-хвосты
                var allUsers = await _userRepository.GetAllAsync();
                var pendingList = allUsers.Where(u => u.IsSynced == 0).ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных юзеров. Инициируем PUSH.");
                    foreach (var user in pendingList)
                    {
                        bool success = await ApiClient.PushUserAsync(user);
                        if (success)
                        {
                            // Сообщаем, что сервер принял
                            EventBus.Publish(this, new IUserSyncActions.Confirmed(user.Id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при синхронизации пользователей: {ex.Message}");
            }
        }

        private async void OnUserInserted(IUserRepo.Inserted msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            // Прямой PUSH, если юзер создан локально
            bool success = await ApiClient.PushUserAsync(msg.User);
            if (success)
            {
                EventBus.Publish(this, new IUserSyncActions.Confirmed(msg.User.Id));
            }
        }

        private async void OnUserUpdated(IUserRepo.Updated msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            bool success = await ApiClient.PushUserAsync(msg.User);
            if (success)
            {
                EventBus.Publish(this, new IUserSyncActions.Confirmed(msg.User.Id));
            }
        }

        private async void OnUserDeleted(IUserRepo.Deleted msg)
        {
            if (msg == null || msg.UserId == Guid.Empty) return;

            bool success = await ApiClient.DeleteUserAsync(msg.UserId);
            if (success)
            {
                EventBus.Publish(this, new IUserSyncActions.DeleteConfirmed(msg.UserId));
            }
        }

        private async void OnUserSyncedOnServer(IUserSyncActions.Confirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Сервер подтвердил профиль '{msg.GuidId}'. Помечаем как синхронизированный.");
            await _userRepository.MarkUserAsSyncedAsync(msg.GuidId);
        }

        private async void OnUserDeleteConfirmedByServer(IUserSyncActions.DeleteConfirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Удаление подтверждено сервером. Стираем локально: {msg.UserId}");
            await _userRepository.DeleteByIdAsync(msg.UserId);
        }
    }
}