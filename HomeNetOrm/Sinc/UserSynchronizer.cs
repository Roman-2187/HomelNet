using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Repositories.Local;

namespace HomeNetOrm.Sinc
{
    /// <summary>
    /// Автоматический фоновый конвейер репликации профилей пользователей.
    /// Реагирует СТРОГО на неотправленные (Unsynced) локальные хвосты базы данных! 🧼
    /// </summary>
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        private readonly IUserRepository _userRepository;

        public UserSynchronizer(IUserRepository userRepository, IEventBus eventBus, ILogger logger)
            : base(eventBus, logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        protected override void SubscribeEvents()
        {
            // Локальные изменения профилей
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);

            // Ответы от сетевого слоя
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserSyncedOnServer>(OnUserSyncedOnServer);
            EventBus.Subscribe<ISiberNetSyncCoordinator.UserDeleteConfirmedByServer>(OnUserDeleteConfirmedByServer);
        }

        protected override void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserInserted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeleted);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserSyncedOnServer>(OnUserSyncedOnServer);
            EventBus.Unsubscribe<ISiberNetSyncCoordinator.UserDeleteConfirmedByServer>(OnUserDeleteConfirmedByServer);
        }

        /// <summary>
        /// 🔥 ГЛАВНЫЙ СЕРДЦЕБИЕНИЕ ДВИЖКА: Опрашивает SQLite и пушит строго грязные данные
        /// </summary>
        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Запуск синхронизации профилей...");

                var syncSupport = _userRepository as ISyncSupport<UserEntity>;
                if (syncSupport == null) return;

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Затягиваем обновления с бэкенда
                // ============================================================================
                Logger.LogInfo($"[{GetType().Name}] Кричим в автобус запрос на скачивание обновлений юзеров...");
                EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPullRequested());

                // ============================================================================
                // 📤 НАПРАВЛЕНИЕ PUSH: Достаем локальные оффлайн-хвосты (IsSynced == 0)
                // ============================================================================
                var pendingSyncList = await syncSupport.GetUnsyncedRecordsAsync();
                var pendingList = pendingSyncList.ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных юзеров с IsSynced == 0. Инициируем PUSH.");
                    foreach (var user in pendingList)
                    {
                        // Один контролируемый пуш на одну грязную запись!
                        EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingSync(user));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при синхронизации пользователей: {ex.Message}");
            }
        }

        private async void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null) return;

            // Если данные прилетели из сети (PULL) — у них IsSynced == 1. Молча мержим их в SQLite.
            if (msg.User.IsSynced == 1)
            {
                var syncSupport = _userRepository as ISyncSupport<UserEntity>;
                if (syncSupport != null)
                {
                    Logger.LogDebug($"[UserSync] Передаем {msg.User.Id} в ORM для сверки.");
                    await syncSupport.MergePulledRecordsAsync([msg.User], EventBus, Logger);
                }
                return;
            }

            // 🎯 САМОДЕЯТЕЛЬНОСТЬ ВЫРЕЗАНА! 
            // Если кто-то локально сделал Insert с IsSynced == 0, мы больше не пушим пакет в сеть сломя голову.
            // Мы даем транзакции спокойно лечь на диск. Конвейер ExecuteTableSyncInternalAsync 
            // сам лениво подхватит эту запись при следующем тике таймера репликации!
        }

        private async void OnUserSyncedOnServer(ISiberNetSyncCoordinator.UserSyncedOnServer msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Сервер подтвердил профиль '{msg.UserId}'. Помечаем как синхронизированный.");

            var syncSupport = _userRepository as ISyncSupport<UserEntity>;
            if (syncSupport != null)
            {
                // Сбрасываем флаг грязной записи в базе данных клиента
                await syncSupport.MarkAsSyncedAsync(msg.UserId);
            }
        }

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            if (msg == null || msg.UserId == Guid.Empty) return;
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingDelete(msg.UserId));
        }

        private async void OnUserDeleteConfirmedByServer(ISiberNetSyncCoordinator.UserDeleteConfirmedByServer msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Удаление подтверждено сервером. Стираем локально: {msg.UserId}");
            await _userRepository.DeleteByIdAsync(msg.UserId);
        }

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            // Логика апдейтов будет аналогично подхватываться через общий пул Unsynced
        }
    }
}
