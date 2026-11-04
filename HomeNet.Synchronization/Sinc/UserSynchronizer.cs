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
    /// Автоматический фоновый конвейер репликации профилей пользователей.
    /// Работает через укрощенный IUserSyncActions и "глупый" IUserRepo! 🧼🦾
    /// </summary>
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        private readonly IUserRepo _userRepository;

        public UserSynchronizer(IUserRepo userRepository, IEventBus eventBus, ILogger logger)
            : base(eventBus, logger)
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

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Затягиваем обновления с бэкенда через короткий рекорд-брат
                // ============================================================================
                Logger.LogInfo($"[{GetType().Name}] Кричим в автобус запрос на скачивание обновлений юзеров...");
                EventBus.Publish(this, new IUserSyncActions.PullRequested());

                // ============================================================================
                // 📤 НАПРАВЛЕНИЕ PUSH: Достаем локальные оффлайн-хвосты (IsSynced == 0)
                // ============================================================================
                // 🔥 ИСПРАВЛЕНО: Вызываем дженерик-экстеншен базового CRUD с указанием сущности!
                var allUsers = await _userRepository.GetAllAsync<UserEntity>();
                var pendingList = allUsers.Where(u => u.IsSynced == 0).ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных юзеров с IsSynced == 0. Инициируем PUSH.");
                    foreach (var user in pendingList)
                    {
                        EventBus.Publish(this, new IUserSyncActions.Pending(user));
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
            if (msg?.User == null) return;

            if (msg.User.IsSynced == 1)
            {
                Logger.LogDebug($"[UserSync] Сохраняем прилетевшего из сети пользователя: {msg.User.Id}");
                await _userRepository.InsertUserAsync(msg.User);
                return;
            }

            EventBus.Publish(this, new IUserSyncActions.Pending(msg.User));
        }

        private async void OnUserSyncedOnServer(IUserSyncActions.Confirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Сервер подтвердил профиль '{msg.GuidId}'. Помечаем как синхронизированный.");

            // 🔥 ИСПРАВЛЕНО: Вызываем специализированный экстеншен маркировки юзеров!
            await _userRepository.MarkUserAsSyncedAsync(msg.GuidId);
        }

        private void OnUserDeleted(IUserRepo.Deleted msg)
        {
            if (msg == null || msg.UserId == Guid.Empty) return;

            EventBus.Publish(this, new IUserSyncActions.DeletePending(msg.UserId));
        }

        private async void OnUserDeleteConfirmedByServer(IUserSyncActions.DeleteConfirmed msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Удаление подтверждено сервером. Стираем локально: {msg.UserId}");

            // 🔥 ИСПРАВЛЕНО: Вызываем дженерик-экстеншен базового CRUD для удаления
            await _userRepository.DeleteByIdAsync<UserEntity>(msg.UserId);
        }

        private void OnUserUpdated(IUserRepo.Updated msg)
        {
            if (msg?.User == null) return;

            if (msg.User.IsSynced == 0)
            {
                EventBus.Publish(this, new IUserSyncActions.Pending(msg.User));
            }
        }
    }
}
