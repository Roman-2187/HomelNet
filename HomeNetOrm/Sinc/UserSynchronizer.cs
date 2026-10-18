using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetOrm.Repositories;
using System;
using System.Threading.Tasks;

namespace HomeNetOrm.Sinc
{
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        private readonly IUserRepository _userRepository;

        public UserSynchronizer(
            IUserRepository userRepository,
            IEventBus eventBus,
            ILogger logger)
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

        protected override async Task ExecuteTableSyncInternalAsync()
        {
            try
            {
                Logger.LogInfo($"[{GetType().Name}] Инициация пакетной сверки профилей через ОРМ...");

                var syncSupport = _userRepository as HomeNetOrm.Interfaces.DbInfrastructure.ISyncSupport<UserEntity>;
                if (syncSupport == null) return;

                var localConn = syncSupport.Connection;
                var localGen = syncSupport.SqlGenerator;

                // ============================================================================
                // 📥 НАПРАВЛЕНИЕ PULL: Скачиваем новые данные с сервера, если они там появились
                // ============================================================================
                // Используем твой готовый метод из RepositorySyncExtensions! 🦾
                // Нам понадобится строка подключения к серверному Postgres для теста
                string remoteConnStr = "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";
                using (var remoteConn = new Npgsql.NpgsqlConnection(remoteConnStr))
                {
                    // Берем твой серверный генератор, чтобы ОРМ со стороны Postgres понимала структуру
                    var remoteGen = localGen; // Временный хак для одинаковых структур таблиц

                    Logger.LogInfo($"[{GetType().Name}] Проверяем обновления на сервере PostgreSQL...");
                    int pulledCount = await syncSupport.PullRepositoryTableAsync(remoteConn, remoteGen); // Вызов твоего метода!

                    if (pulledCount > 0)
                    {
                        Logger.LogInfo($"[{GetType().Name}] ✅ Успешно затянуто {pulledCount} новых пользователей с сервера в SQLite!");
                    }
                }
                // ============================================================================

                // 📤 НАПРАВЛЕНИЕ PUSH: (Твой текущий рабочий код оффлайн-отправки хвостов)
                string sql = HomeNetOrm.DBProviders.Sqlite.SqliteSincExtensions.GenerateSelectUnsyncedSqlite(localGen);
                var unsyncedUsers = await localConn.QueryAsync<UserEntity>(sql);
                var pendingList = unsyncedUsers.ToList();

                if (pendingList.Count > 0)
                {
                    Logger.LogWarning($"[{GetType().Name}] ОРМ обнаружила {pendingList.Count} неотправленных юзеров. Инициируем отправку.");
                    foreach (var user in pendingList)
                    {
                        EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingSync(user));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Сбой при пакетной сверке пользователей: {ex.Message}");
            }
        }




        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            Logger.LogInfo($"[UserSync] Локальный профиль '{name}' (ID: {msg.User.Id}). Отправляем запрос на синхронизацию.");

            // Кидаем в автобус команду на отправку профиля наружу
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingSync(msg.User));
        }

        private async void OnUserSyncedOnServer(ISiberNetSyncCoordinator.UserSyncedOnServer msg)
        {
            if (msg == null) return;

            Logger.LogInfo($"[UserSync] Сервер подтвердил профиль '{msg.UserId}'. Помечаем как синхронизированный.");
            await _userRepository.MarkAsSyncedAsync(msg.UserId);
        }

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            if (msg == null || msg.UserId == Guid.Empty) return;

            Logger.LogWarning($"[UserSync] Локальный запрос на удаление пользователя с ID '{msg.UserId}'. Передаем задачу в сеть.");

            // Пуляем в шину: "Нужно удалить этого чувака с сервера!"
            EventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingDelete(msg.UserId));
        }

        private async void OnUserDeleteConfirmedByServer(ISiberNetSyncCoordinator.UserDeleteConfirmedByServer msg)
        {
            if (msg == null) return;

            Logger.LogInfo($"[UserSync] Сеть подтвердила удаление {msg.UserId} с сервера. Теперь удаляем из локальной базы.");

            // Только теперь безопасно удаляем из SQLite
            await _userRepository.DeleteByIdAsync(msg.UserId);
        }

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;
            Logger.LogInfo($"[UserSync] Перехвачено обновление профиля '{msg.User.FirstName}'");
        }
    }
}
