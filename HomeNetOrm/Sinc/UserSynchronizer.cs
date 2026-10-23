using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Repositories;


namespace HomeNetOrm.Sinc
{
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

            // Ответы от сетевого слоя (PUSH и PULL обрабатываются одинаково через вставку!)
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

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Запуск синхронизации профилей...");

                var syncSupport = _userRepository as ISyncSupport<UserEntity>;
                if (syncSupport == null) return;

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Просто просим автобус затянуть данные!
                // ============================================================================
                Logger.LogInfo($"[{GetType().Name}] Кричим в автобус запрос на скачивание обновлений юзеров...");

                // Публикуем событие запроса. Хандлер его поймает, сходит на сервер, 
                // получит пачку и вернет её обратно в этот же синхронизатор через OnUserInserted!
                EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPullRequested());

                // ============================================================================
                // 📤 НАПРАВЛЕНИЕ PUSH: Достаем локальные оффлайн-хвосты через наше чистое расширение
                // ============================================================================
                var pendingSyncList = await syncSupport.GetUnsyncedRecordsAsync();
                var pendingList = pendingSyncList.ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] Обнаружено {pendingList.Count} неотправленных юзеров. Инициируем PUSH.");
                    foreach (var user in pendingList)
                    {
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

            if (msg.User.IsSynced == 1)
            {
                var syncSupport = _userRepository as ISyncSupport<UserEntity>;
                if (syncSupport != null)
                {
                    // 🎯 Добавляем информативный лог перед записью в SQLite
                    Logger.LogDebug($"[UserSync] Передаем {msg.User.Id} в ORM для сверки.");

                    // Передаем и EventBus, и наш Logger 🎯
                    await syncSupport.MergePulledRecordsAsync([msg.User], EventBus, Logger);
                }
                return;
            }

            // Если данные родились локально (IsSynced == 0), отправляем их в сеть
            Logger.LogInfo($"[UserSync] Локальный профиль '{msg.User.FirstName}'. Отправляем запрос на синхронизацию.");
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingSync(msg.User));
        }


        private async void OnUserSyncedOnServer(ISiberNetSyncCoordinator.UserSyncedOnServer msg)
        {
            if (msg == null) return;
            Logger.LogInfo($"[UserSync] Сервер подтвердил профиль '{msg.UserId}'. Помечаем как синхронизированный.");
            // === ЯВНО ПОДСКАЗЫВАЕМ ТИП КОМПИЛЯТОРУ 🎯 ===
            // Приводим репозиторий к интерфейсу поддержки синхронизации ОРМ
            var syncSupport = _userRepository as ISyncSupport<UserEntity>;

            if (syncSupport != null)
            {
                // Теперь компилятор видит нужный тип 'this' и метод расширения сработает как часы! 🎯
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

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg) { /* Логика апдейтов */ }
    }
}
