using System.Data.Common;
using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// 🌐 ПУБЛИЧНЫЙ УДАЛЕННЫЙ РЕПОЗИТОРИЙ СИНХРОНИЗАЦИИ ДЕВАЙСОВ ДЛЯ СЕРВЕРА
    /// </summary>
    public class DeviceSyncRepository : IDeviceSyncRepository
    {
        private readonly DbConnection _connection;
        private readonly IRemoteDeviceSyncSqlGenerator _sqlGenerator;
        // 🎯 Никакого хардкода! Переменная для динамического имени из DI
        private readonly string _tableName;

        // Принимаем имя таблицы сверху из DI-контейнера при сборке машины
        public DeviceSyncRepository(DbConnection connection, IRemoteDeviceSyncSqlGenerator sqlGenerator, string tableName )
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _tableName = tableName;
        }

        /// <summary>
        /// Вытаскивает UserId по уникальному DeviceId девайса (ЧЕРЕЗ ГЕНЕРАТОР)
        /// </summary>
        public async Task<Guid> GetUserIdByDeviceAsync(Guid deviceId)
        {
            // Передаем в генератор имя, полученное из нашего DI 🦾
            string sql = _sqlGenerator.GenerateSelectUserIdByDevice(_tableName);

            return await _connection.QueryFirstOrDefaultAsync<Guid>(sql, new { deviceId });
        }

        /// <summary>
        /// Обновление чекпоинта (ЧЕРЕЗ ГЕНЕРАТОР)
        /// </summary>
        public async Task UpdateDeviceCheckpointAsync(Guid deviceId, Guid userId, DateTimeOffset now)
        {
            // Передаем в генератор имя, полученное из нашего DI 🦾
            string sql = _sqlGenerator.GenerateDeviceSyncUpsert(_tableName);

            await _connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                deviceId = deviceId,
                userId = userId,
                Now = now
            });
        }
    }
}
