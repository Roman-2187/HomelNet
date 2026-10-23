using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity; 
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// 🌐 ПУБЛИЧНЫЙ УДАЛЕННЫЙ РЕПОЗИТОРИЙ СИНХРОНИЗАЦИИ ДЕВАЙСОВ ДЛЯ СЕРВЕРА.
    /// Полный автопилот: работает без хардкодных строк и протаскивания имен таблиц! 🎯
    /// </summary>
    public class DeviceSyncRepository : IDeviceSyncRepository
    {
        private readonly DbConnection _connection;
        private readonly IRemoteDeviceSyncSqlGenerator _sqlGenerator;

        public DeviceSyncRepository(DbConnection connection, IRemoteDeviceSyncSqlGenerator sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        /// <summary>
        /// Вытаскивает UserId по уникальному DeviceId девайса
        /// </summary>
        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            string sql = _sqlGenerator.GenerateSelectUserIdByDevice();
            return await _connection.QueryFirstOrDefaultAsync<Guid>(sql, new { DeviceId = deviceId });
        }

        /// <summary>
        /// Обновление чекпоинта (UPSERT на сервере)
        /// </summary>
        public async Task UpdateDeviceCheckpointAsync(Guid deviceId, Guid userId, DateTimeOffset now)
        {
            string sql = _sqlGenerator.GenerateDeviceSyncUpsert();

            // 🔥 ФИКС: Передаем СУЩНОСТЬ строго по твоей POCO-модели!
            // Никаких Id и LastSyncedAt — только чистая правда структуры!
            var syncData = new DeviceSyncEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                UpdatedAt = now,
                Version = 0 // По дефолту ноль, Postgres триггер сам увеличит её при вставке/апдейте
            };

            await _connection.ExecuteAsync(sql, syncData);
        }
    }
}
