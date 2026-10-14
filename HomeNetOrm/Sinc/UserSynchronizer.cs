using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.Extensions;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Sinc;
using System.Data.Common;

namespace HomeNetOrm.Sinc
{
    public class UserSynchronizer : BaseSynchronizer<UserEntity>
    {
        public UserSynchronizer(
            IDbEngine localEngine, IDbEngine remoteEngine,
            IDbContextContainer contextContainer, IEventBus eventBus, ILogger logger)
            : base(localEngine, remoteEngine, contextContainer, eventBus, logger) { }

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

        protected override async Task ExecuteTableSyncInternalAsync(DbConnection local, DbConnection remote)
        {
            int usersPulled = await local.PullTableAsync(remote, LocalEngine.GetGenerator<UserEntity>(), RemoteEngine.GetGenerator<UserEntity>());
            if (usersPulled > 0) Logger.LogInfo($"[UserSync] Из сети скачано новых пользователей: {usersPulled}");

            int usersPushed = await local.PushTableAsync(remote, LocalEngine.GetGenerator<UserEntity>(), RemoteEngine.GetGenerator<UserEntity>());
            if (usersPushed > 0) Logger.LogInfo($"[UserSync] На сервер успешно выгружено оффлайн-пользователей: {usersPushed}");
        }

        private void OnUserInserted(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            Logger.LogInfo($"[UserSync] Перехвачен пуш профиля '{name}' (ID: {msg.User.Id})");

            var remoteGen = RemoteEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateInsert(), msg.User), $"User Insert ({name})");
        }

        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            if (msg?.User == null || msg.User.IsSynced == 1) return;

            string name = msg.User.FirstName ?? "Без имени";
            var remoteGen = RemoteEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateUpdate(), msg.User), $"User Update ({name})");
        }

        private void OnUserDeleted(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            Logger.LogWarning($"[UserSync] Перехвачено удаление пользователя с ID '{msg.UserId}'");

            var remoteGen = RemoteEngine.GetGenerator<UserEntity>();
            ExecuteSyncTask(async (remote) => await remote.ExecuteAsync(remoteGen.GenerateDelete(), new { id = msg.UserId }), "User Delete");
        }
    }
}
