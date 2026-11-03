using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics; // Твой логгер со строки 2
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories; // Подключаем наш плоский IRepository
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Remote
{
    /// <summary>
    /// 🌐 ПУБЛИЧНЫЙ УДАЛЕННЫЙ РЕПОЗИТОРИЙ СИНХРОНИЗАЦИИ ДЕВАЙСОВ ДЛЯ СЕРВЕРА.
    /// Реализует плоский IRepository для управления пулом без дженериков в ядре СУБД! 🎯🦾
    /// </summary>
    public class DeviceSyncRepository : IDeviceSyncRepository, IRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly IRemoteDeviceSyncSqlGenerator _sqlGenerator;

        public ILogger Logger { get; }

        internal DeviceSyncRepository(Func<DbConnection> connectionFactory, IRemoteDeviceSyncSqlGenerator sqlGenerator, ILogger logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectUserIdByDevice(deviceId);

            // 🔥 Выполняем через внутренний безопасный пул ExecuteAsync
            return await ExecuteAsync(db => db.QueryFirstOrDefaultAsync<Guid>(cmd.Sql, cmd.Params));
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

            // 🔥 Точно так же атомарно прогоняем через пул команд
            await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, syncData));
        }

        // =========================================================================
        // 🦾 Реализация интерфейса IRepository (Управление изолированными using-блоками)
        // =========================================================================

        public async Task<TResult> ExecuteAsync<TResult>(Func<DbConnection, Task<TResult>> query)
        {
            using (var db = _connectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                return await query(db);
            }
        }

        public async Task ExecuteAsync(Func<DbConnection, Task> command)
        {
            using (var db = _connectionFactory())
            {
                if (db.State != ConnectionState.Open) await db.OpenAsync();
                await command(db);
            }
        }
    }
}
