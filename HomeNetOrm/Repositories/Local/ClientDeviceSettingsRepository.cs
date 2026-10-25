using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Repositories.Local
{
    public class ClientDeviceSettingsRepository
    {
        private readonly DbConnection _connection;
        private readonly ILocalDeviceSettingsSqlGenerator _sqlGenerator;

        internal ClientDeviceSettingsRepository(DbConnection connection, ILocalDeviceSettingsSqlGenerator sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        /// <summary>
        /// Вытаскивает или генерирует новый чистый Guid железки при самом первом старте
        /// </summary>
        public async Task<Guid> GetOrCreateDeviceIdAsync()
        {
            SqlQueryCommand selectCmd = _sqlGenerator.GenerateSelectByKey();

            // Вытаскиваем всю сущность целиком, чтобы проверить её наличие
            var settings = await _connection.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(selectCmd.Sql, selectCmd.Params);

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

            // Сохраняем первую дефолтную запись устройства
            await SaveSettingsAsync(newSettings);

            return newDeviceId;
        }

        /// <summary>
        /// 🔥 НАШ НОВЫЙ МЕТОД: Возвращает полную строку настроек (включая логин/пароль сессии)
        /// </summary>
        public async Task<DeviceSettingsEntity?> GetSettingsAsync()
        {
            SqlQueryCommand selectCmd = _sqlGenerator.GenerateSelectByKey();

            // Запрашиваем из SQLite всю запись целиком
            return await _connection.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(selectCmd.Sql, selectCmd.Params);
        }

        /// <summary>
        /// 🔥 НАШ ВТОРОЙ МЕТОД: Жесткий и безопасный UPSERT на две схемы
        /// </summary>
        public async Task SaveSettingsAsync(DeviceSettingsEntity settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            settings.UpdatedAt = DateTimeOffset.UtcNow;

            // Генератор выдает готовую строку со всеми Camel/Snake маппингами
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();

            // Dapper хавает объект settings напрямую, свойства сопоставятся на лету
            await _connection.ExecuteAsync(insertCmd.Sql, settings);
        }
    }
}
