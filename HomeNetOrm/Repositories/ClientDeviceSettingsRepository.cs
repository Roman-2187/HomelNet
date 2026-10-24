using System.Data.Common;
using Dapper;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Repositories
{
    public class ClientDeviceSettingsRepository
    {
        private readonly DbConnection _connection;
        private readonly ILocalDeviceSettingsSqlGenerator _sqlGenerator;

        // 🔥 ФИКС ВИДИМОСТИ: Конструктор становится internal!
        internal ClientDeviceSettingsRepository(DbConnection connection, ILocalDeviceSettingsSqlGenerator sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetOrCreateDeviceIdAsync()
        {
            SqlQueryCommand selectCmd = _sqlGenerator.GenerateSelectByKey();

            // 🎯 Вызываем команду через точку. Параметры подхватятся автоматически из cmd.Params
            string? savedIdStr = await _connection.QueryFirstOrDefaultAsync<string>(selectCmd.Sql, selectCmd.Params);

            if (!string.IsNullOrEmpty(savedIdStr) && Guid.TryParse(savedIdStr, out Guid savedGuid))
            {
                return savedGuid;
            }

            Guid newDeviceId = Guid.NewGuid();
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();

            var newSettings = new DeviceSettingsEntity
            {
                DeviceId = newDeviceId,
                UserId = Guid.Empty,
                Email = string.Empty,
                Password = string.Empty,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _connection.ExecuteAsync(insertCmd.Sql, newSettings);

            return newDeviceId;
        }
    }
}
