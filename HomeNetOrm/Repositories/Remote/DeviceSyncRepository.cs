using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Remote
{
    /// <summary>
    /// 🌐 ПУБЛИЧНЫЙ УДАЛЕННЫЙ РЕПОЗИТОРИЙ СИНХРОНИЗАЦИИ ДЕВАЙСОВ ДЛЯ СЕРВЕРА.
    /// Полный автопилот: работает без хардкодных строк! 🎯
    /// </summary>
    public class DeviceSyncRepository : IDeviceSyncRepository
    {
        private readonly DbConnection _connection;
        private readonly IRemoteDeviceSyncSqlGenerator _sqlGenerator;

        // 🔥 ФИКС ВИДИМОСТИ: Конструктор становится internal!
        internal DeviceSyncRepository(DbConnection connection, IRemoteDeviceSyncSqlGenerator sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            // 🎯 Забираем готовую команду с зашитым deviceId параметром из генератора
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectUserIdByDevice(deviceId);
            return await _connection.QueryFirstOrDefaultAsync<Guid>(cmd.Sql, cmd.Params);
        }

        public async Task UpdateDeviceCheckpointAsync(Guid deviceId, Guid userId, DateTimeOffset now)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateDeviceSyncUpsert();

            var syncData = new DeviceSyncEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                UpdatedAt = now,
                Version = 0
            };

            await _connection.ExecuteAsync(cmd.Sql, syncData);
        }
    }
}
