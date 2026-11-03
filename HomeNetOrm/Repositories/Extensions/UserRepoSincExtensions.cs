using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Generators.Extensions; // Подключаем UserSyncSqlGeneratorExtensions
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Extensions
{
    public static class UserRepoSincExtensions
    {
        // 📥 Выгрузка пакета измененных профилей для отправки
        public static async Task<IEnumerable<UserEntity>> GetUsersForSyncAsync(this IUserRepo repo, DateTimeOffset checkpoint)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateSelectUsersForSync(checkpoint);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

        // 💾 Накатка пакета входящих изменений через UPSERT
        public static async Task SaveOrSyncUserAsync(this IUserRepo repo, UserEntity user)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateUserUpsert();

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
        }

        // 🎯 Универсальная фиксация флага IsSynced = 1 по нашему плоскому методу
        public static async Task MarkUserAsSyncedAsync(this IUserRepo repo, Guid id)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateUpdateMarkAsSynced(id);

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));
            baseRepo.Logger.LogInfo($"[UserEntity] Профиль {id} успешно засинхронен.");
        }
    }
}
