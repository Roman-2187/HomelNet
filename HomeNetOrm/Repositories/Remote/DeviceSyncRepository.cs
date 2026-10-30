using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Repositories.Remote
{
    /// <summary>
    /// 🌐 ПУБЛИЧНЫЙ УДАЛЕННЫЙ РЕПОЗИТОРИЙ СИНХРОНИЗАЦИИ ДЕВАЙСОВ ДЛЯ СЕРВЕРА.
    /// Полный асинхронный автопилот без риска взаимных блокировок! 🎯
    /// </summary>
    public class DeviceSyncRepository : IDeviceSyncRepository
    {
        private readonly Func<DbConnection> _connectionFactory; // 🔥 Меняем тип на многопоточную фабрику
        private readonly IRemoteDeviceSyncSqlGenerator _sqlGenerator;

        internal DeviceSyncRepository(Func<DbConnection> connectionFactory, IRemoteDeviceSyncSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectUserIdByDevice(deviceId);

            // Открываем изолированное соединение строго на время выполнения SQL-запроса
            using (var db = _connectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                return await db.QueryFirstOrDefaultAsync<Guid>(cmd.Sql, cmd.Params);
            }
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

            using (var db = _connectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                await db.ExecuteAsync(cmd.Sql, syncData);
            }
        }
    }
}
