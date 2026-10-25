using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IDeviceSettingsService
    {
        /// <summary>
        /// Возвращает чистый GUID текущего железа
        /// </summary>
        Task<Guid> GetDeviceIdAsync();

        /// <summary>
        /// Возвращает полную сущность настроек девайса (включая авторизованную сессию)
        /// </summary>
        Task<DeviceSettingsEntity?> GetCurrentSettingsAsync();

        /// <summary>
        /// Фиксирует или обновляет сессию пользователя на этом устройстве
        /// </summary>
        Task SaveSessionAsync(Guid userId, string email, string password);

        /// <summary>
        /// Очищает учетные данные сессии (разлогин устройства)
        /// </summary>
        Task ClearSessionAsync();
    }
}
