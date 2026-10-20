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

        /// <summary>
        /// Безопасно вытягивает существующий или генерирует новый Device ID в SQLite
        /// </summary>
        public async Task<Guid> GetOrCreateDeviceIdAsync()
        {
            // 1. Запрашиваем SQL-строку селекта у локального генератора
            string selectSql = _sqlGenerator.GenerateSelectByKey();

            string? savedIdStr = await _connection.QueryFirstOrDefaultAsync<string>(selectSql, new { Key = "DeviceId" });

            // Если в базе уже лежит готовый ID девайса — парсим его из строки и отдаем
            if (!string.IsNullOrEmpty(savedIdStr) && Guid.TryParse(savedIdStr, out Guid savedGuid))
            {
                return savedGuid;
            }

            // 2. Если запустились первый раз и ID нет — генерируем новый Guid
            Guid newDeviceId = Guid.NewGuid();

            // Запрашиваем SQL-строку вставки/апдейта у локального генератора
            string insertSql = _sqlGenerator.GenerateInsertOrUpdate();

            await _connection.ExecuteAsync(insertSql, new { Key = "DeviceId", Value = newDeviceId.ToString() });

            return newDeviceId;
        }
    }
}
