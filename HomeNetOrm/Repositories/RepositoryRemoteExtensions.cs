using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.SyncedGenerators; // 🎯 Подтягиваем папки конкретных Postgres-генераторов
using HomeNetOrm.DBProviders.Postgres.SincedGenerators;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories
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

            // 🎯 Кастуем строго к конкретному серверному Postgres-генератору сообщений!
            if (syncSupport.SqlGenerator is not SyncedPostgresMessageSqlGenerator messageGen)
                return Array.Empty<MessageEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            // Вызываем через точку родной метод класса. Никакого оверинжиниринга интерфейсов!
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

            // 🎯 Так как TEntity может быть либо UserEntity, либо другой сущностью репликации,
            // мы проверяем тип и кастуем к конкретному серверному исполнителю:
            if (typeof(TEntity) == typeof(UserEntity) &&
                syncSupport.SqlGenerator is SyncedPostgresUserSqlGenerator userGen)
            {
                cmd = userGen.GenerateSelectUpdatesByTimestamp(checkpoint);
            }
            else
            {
                // Если прилетела другая сущность, которую сервер не реплицирует по датам — выходим
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

            // Кастуем к конкретному Postgres-генератору синхронизации девайсов
            var deviceGen = syncSupport.SqlGenerator as PostgresDeviceSyncSqlGenerator;
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
