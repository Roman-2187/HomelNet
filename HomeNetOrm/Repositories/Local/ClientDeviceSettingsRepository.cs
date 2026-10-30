using System.Data.Common;
using Dapper;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Repositories.Local
{
    public class ClientDeviceSettingsRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly ILocalDeviceSettingsSqlGenerator _sqlGenerator;

        internal ClientDeviceSettingsRepository(Func<DbConnection> connectionFactory, ILocalDeviceSettingsSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetOrCreateDeviceIdAsync()
        {
            SqlQueryCommand selectCmd = _sqlGenerator.GenerateSelectByKey();

            using (var db = _connectionFactory())
            {
                if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

                var settings = await db.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(selectCmd.Sql, selectCmd.Params);

                if (settings != null && settings.DeviceId != Guid.Empty)
                {
                    return settings.DeviceId;
                }

                Guid newDeviceId = Guid.NewGuid();
                var newSettings = new DeviceSettingsEntity
                {
                    DeviceId = newDeviceId,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    Password = string.Empty,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                // Вызываем метод сохранения в контексте этого же репозитория
                await SaveSettingsInternalAsync(db, newSettings);
                return newDeviceId;
            }
        }

        public async Task<DeviceSettingsEntity?> GetSettingsAsync()
        {
            SqlQueryCommand selectCmd = _sqlGenerator.GenerateSelectByKey();
            using (var db = _connectionFactory())
            {
                if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
                return await db.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(selectCmd.Sql, selectCmd.Params);
            }
        }

        public async Task SaveSettingsAsync(DeviceSettingsEntity settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            using (var db = _connectionFactory())
            {
                if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
                await SaveSettingsInternalAsync(db, settings);
            }
        }

        private async Task SaveSettingsInternalAsync(DbConnection db, DeviceSettingsEntity settings)
        {
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();
            await db.ExecuteAsync(insertCmd.Sql, settings);
        }
    }
}
