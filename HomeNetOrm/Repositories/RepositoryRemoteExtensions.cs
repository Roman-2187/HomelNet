using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators; // Контракт синхронизации
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

            // Кастуем внутренний генератор к нашему интерфейсу синхронизации Postgres
            if (syncSupport.SqlGenerator is not ISyncSqlGenerator<MessageEntity> syncGen)
                return Array.Empty<MessageEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            string sql = ((dynamic)syncGen).GenerateSelectMessagesForSync();

            // 🔥 ФИКС: Вместо хардкодных анонимных параметров передаем СУЩНОСТЬ!
            // Генератор сообщений ожидает, что userId лежит в SenderId, а чекпоинт — в UpdatedAt.
            var syncMessage = new MessageEntity
            {
                SenderId = userId,
                UpdatedAt = checkpoint
            };

            return await serverConn.QueryAsync<MessageEntity>(sql, syncMessage);
        }

        public static async Task<IEnumerable<TEntity>> GetUpdatesSinceAsync<TEntity>(
            this IRepository<TEntity> repository,
            DateTimeOffset checkpoint) where TEntity : class
        {
            var syncSupport = repository as ISyncSupport<TEntity>;
            if (syncSupport == null) return Array.Empty<TEntity>();

            if (syncSupport.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
                return Array.Empty<TEntity>();

            DbConnection serverConn = syncSupport.Connection;
            if (serverConn.State != ConnectionState.Open) await serverConn.OpenAsync();

            string sql = ((dynamic)syncGen).GenerateSelectUpdatesByTimestamp();

            // 🔥 ФИКС: Создаем объект сущности динамически через рефлексию (или активатор)
            // и пишем чекпоинт времени в её свойство UpdatedAt, так как генераторы ждут именно его!
            var syncEntity = Activator.CreateInstance<TEntity>();
            var property = typeof(TEntity).GetProperty(nameof(UserEntity.UpdatedAt));
            if (property != null && property.CanWrite)
            {
                property.SetValue(syncEntity, checkpoint);
            }

            return await serverConn.QueryAsync<TEntity>(sql, syncEntity);
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

            var deviceGen = syncSupport.SqlGenerator as IRemoteDeviceSyncSqlGenerator;
            if (deviceGen == null) return;

            // 🔥 ФИКС: Убрали протаскивание "device_sync" строкой, метод больше не принимает параметры!
            string sql = deviceGen.GenerateDeviceSyncUpsert();

            // 🔥 ФИКС: Передаем СУЩНОСТЬ строго по твоей чистой POCO-модели DeviceSyncEntity
            // Никаких лишних полей Id — параметры сойдутся с DapperParams генератора один в один!
            var syncData = new DeviceSyncEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                UpdatedAt = now,
                Version = 0
            };

            await serverConn.ExecuteAsync(sql, syncData);
        }
    }
}
