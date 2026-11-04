using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators; // Подключаем напрямую классы генераторов
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Models;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Repositories.Remote
{
    /// <summary>
    /// Удаленный репозиторий синхронизации. Работает напрямую с классом генератора для Postgres. 🌐🦾
    /// </summary>
    public class DeviceSyncRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly DeviceSyncSqlGenerator _sqlGenerator; // Твой конкретный класс генератора!

        internal DeviceSyncRepository(Func<DbConnection> connectionFactory, DeviceSyncSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectUserIdByDevice(deviceId);

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<Guid>(cmd.Sql, cmd.Params);
        }

        public async Task UpdateDeviceCheckpointAsync(Guid deviceId, Guid userId, DateTimeOffset now)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateDeviceSyncUpsert();

            var syncData = new DeviceSyncEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                UpdatedAt = now,
               
            };

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, syncData);
        }
    }
}
