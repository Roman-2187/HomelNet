using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetOrm.Interfaces.Generators;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// 📱 ЛОКАЛЬНЫЙ ПУБЛИЧНЫЙ РЕПОЗИТОРИЙ НАСТРОЕК ДЕВАЙСА (ДЛЯ КЛИЕНТА АВАЛОНИИ)
    /// </summary>
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

            string? savedIdStr = await _connection.QueryFirstOrDefaultAsync<string>(selectSql, new { Key = "DeviceId" });

            if (!string.IsNullOrEmpty(savedIdStr) && Guid.TryParse(savedIdStr, out Guid savedGuid))
            {
                return savedGuid;
            }

            Guid newDeviceId = Guid.NewGuid();
            string insertSql = _sqlGenerator.GenerateInsertOrUpdate();

            // 🎯 ДОБАВЛЯЕМ ПАРАМЕТРЫ ДЛЯ ОБХОДА NOT NULL НА УРОВНЕ C#
            await _connection.ExecuteAsync(insertSql, new
            {
                Key = "DeviceId",
                Value = newDeviceId.ToString(),
                UserId = Guid.Empty,          // Передаем пустой Guid
                Email = string.Empty,        // Пустая строка
                Password = string.Empty      // Пустая строка
            });

            return newDeviceId;
        }

    }
}
