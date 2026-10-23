using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators; // 🎯 Контракт синхронизации
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

            // === ЗАЩИТА ТИПОВ ===
            // Кастуем внутренний генератор ОРМ к нашему интерфейсу синхронизации Postgres
            if (syncSupport.SqlGenerator is not ISyncSqlGenerator<MessageEntity> syncGen)
                return Array.Empty<MessageEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            // Так как мы перенесли метод GenerateSelectMessagesForSync в сам SyncedPostgresMessageSqlGenerator,
            // кастуем syncGen к конкретному типу наследника или динамику, чтобы забрать кастомный SQL чата
            string sql = ((dynamic)syncGen).GenerateSelectMessagesForSync();

            return await serverConn.QueryAsync<MessageEntity>(sql, new { userId, clientCheckpoint = checkpoint });
        }

        public static async Task<IEnumerable<TEntity>> GetUpdatesSinceAsync<TEntity>(
            this IRepository<TEntity> repository,
            DateTimeOffset checkpoint) where TEntity : class
        {
            var syncSupport = repository as ISyncSupport<TEntity>;
            if (syncSupport == null) return Array.Empty<TEntity>();

            // === ЗАЩИТА ТИПОВ ===
            if (syncSupport.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
                return Array.Empty<TEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            // Забираем кастомную строку генерации апдейтов из наследника
            string sql = ((dynamic)syncGen).GenerateSelectUpdatesByTimestamp();

            return await serverConn.QueryAsync<TEntity>(sql, new { clientCheckpoint = checkpoint });
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

            // Для девайсов у нас выделенный IRemoteDeviceSyncSqlGenerator, дергаем его напрямую
            var deviceGen = syncSupport.SqlGenerator as IRemoteDeviceSyncSqlGenerator;
            if (deviceGen == null) return;

            // Наш репозиторий девайсов сам прокинет имя таблицы, которое получил из DI! 🦾
            string sql = deviceGen.GenerateDeviceSyncUpsert("device_sync");

            await serverConn.ExecuteAsync(sql, new { Id = Guid.NewGuid(), deviceId, userId, Now = now });
        }
    }
}
