using System.Data;
using System.Data.Common;
using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Generators.Sinc;

namespace HomeNetOrm.Repositories.Remote
{
    public static class RepositoryRemoteExtensions
    {
        public static async Task<IEnumerable<MessageEntity>> GetFreshMessagesForUserAsync(
            this IRepository<MessageEntity> repository,
            Guid userId,
            DateTimeOffset checkpoint)
        {
            var syncSupport = repository as ISyncSupport<MessageEntity>;
            if (syncSupport == null) return Array.Empty<MessageEntity>();

            // 🎯 Кастуем строго к твоему готовому SyncedMessageSqlGenerator!
            if (syncSupport.SqlGenerator is not SyncedMessageSqlGenerator messageGen)
                return [];

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            // Дергаем готовый рабочий метод класса
            SqlQueryCommand cmd = messageGen.GenerateSelectMessagesForSync(userId, checkpoint);

            return await serverConn.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params);
        }

        public static async Task<IEnumerable<TEntity>> GetUpdatesSinceAsync<TEntity>(
            this IRepository<TEntity> repository,
            DateTimeOffset checkpoint) where TEntity : class
        {
            var syncSupport = repository as ISyncSupport<TEntity>;
            if (syncSupport == null) return Array.Empty<TEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            SqlQueryCommand cmd;

            // 🎯 Проверяем тип сущности и кастуем к твоему готовому SyncedUserSqlGenerator!
            if (typeof(TEntity) == typeof(UserEntity) &&
                syncSupport.SqlGenerator is SyncedUserSqlGenerator userGen)
            {
                cmd = userGen.GenerateSelectUpdatesByTimestamp(checkpoint);
            }
            else
            {
                return Array.Empty<TEntity>();
            }

            return await serverConn.QueryAsync<TEntity>(cmd.Sql, cmd.Params);
        }

        public static async Task UpdateDeviceCheckpointAsync(
            this IRepository<DeviceSyncEntity> repository,
            Guid deviceId,
            Guid userId,
            DateTimeOffset now)
        {
            var syncSupport = repository as ISyncSupport<DeviceSyncEntity>;
            if (syncSupport == null) return;

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            // Кастуем к твоему универсальному генератору синхронизации девайсов
            var deviceGen = syncSupport.SqlGenerator as IRemoteDeviceSyncSqlGenerator;
            if (deviceGen == null) return;

            SqlQueryCommand cmd = deviceGen.GenerateDeviceSyncUpsert();

            var syncData = new DeviceSyncEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                UpdatedAt = now,
                Version = 0
            };

            await serverConn.ExecuteAsync(cmd.Sql, syncData);
        }
    }
}
