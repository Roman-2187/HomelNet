using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;

namespace HomeNetOrm.Repositories
{
    public class ClientDeviceSettingsRepository
    {
        private readonly DbConnection _connection;
        private readonly ILocalDeviceSettingsSqlGenerator _sqlGenerator;

        public ClientDeviceSettingsRepository(DbConnection connection, ILocalDeviceSettingsSqlGenerator sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<Guid> GetOrCreateDeviceIdAsync()
        {
            string selectSql = _sqlGenerator.GenerateSelectByKey();

            var querySettings = new DeviceSettingsEntity();

            // Передаем объект-пустышку, Dapper сам сопоставит свойства
            string? savedIdStr = await _connection.QueryFirstOrDefaultAsync<string>(selectSql, querySettings);

            if (!string.IsNullOrEmpty(savedIdStr) && Guid.TryParse(savedIdStr, out Guid savedGuid))
            {
                return savedGuid;
            }

            Guid newDeviceId = Guid.NewGuid();
            string insertSql = _sqlGenerator.GenerateInsertOrUpdate();

            // 🎯 Чистая сущность, никаких текстовых Id и .ToString()!
            var newSettings = new DeviceSettingsEntity
            {
                DeviceId = newDeviceId,
                UserId = Guid.Empty, // Спокойно пишем Guid.Empty, так как в сущности это Guid?
                Email = string.Empty,
                Password = string.Empty,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Просто отдаем генератору готовую модель
            await _connection.ExecuteAsync(insertSql, newSettings);

            return newDeviceId;
        }


    }
}
