using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Repositories.Local;
using HomeNetCore.Extensions;
namespace HomeNetServices.Identity
{
    public class DeviceSettingsService : IDeviceSettingsService
    {
        private readonly ClientDeviceSettingsRepository _deviceSettingsRepo;
        private readonly ILogger _logger;

        // 🔥 НАШ ВЕЧНЫЙ СТЕЙТ В ОПЕРАТИВКЕ
        private DeviceSettingsEntity? _cachedSettings;

        public DeviceSettingsService(ClientDeviceSettingsRepository deviceSettingsRepo, ILogger logger)
        {
            _deviceSettingsRepo = deviceSettingsRepo ?? throw new ArgumentNullException(nameof(deviceSettingsRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 🎯 ТВОЯ ИДЕЯ В ДЕЙСТВИИ: Инициализируем настройки железа прямо при рождении сервиса!
            InitializeSettingsFromDb();
        }

        private void InitializeSettingsFromDb()
        {
            try
            {
                // Запускаем синхронно в фоне или через Task.Run, чтобы не вешать поток сборки DI
                Task.Run(async () =>
                {
                    // Репозиторий сам проверит SQLite и создаст запись, если её нет
                    Guid deviceId = await _deviceSettingsRepo.GetOrCreateDeviceIdAsync();
                    _cachedSettings = await _deviceSettingsRepo.GetSettingsAsync();

                    _logger.LogInfo($"[DeviceSettingsService] Сервер настроек прогрет. Активный Device ID в памяти: {deviceId}");
                }).Wait(); // Блокируем до завершения, чтобы гарантировать готовность стейта
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettingsService] Ошибка ленивой генерации Device ID в конструкторе: {ex.Message}");
            }
        }

        public async Task<Guid> GetDeviceIdAsync()
        {
            if (_cachedSettings == null) await GetCurrentSettingsAsync();
            return _cachedSettings?.DeviceId ?? Guid.Empty;
        }

        public async Task<DeviceSettingsEntity?> GetCurrentSettingsAsync()
        {
            if (_cachedSettings == null)
            {
                _cachedSettings = await _deviceSettingsRepo.GetSettingsAsync();
            }
            return _cachedSettings;
        }

        public async Task SaveSessionAsync(Guid userId, string email, string password)
        {
            try
            {
                var settings = await GetCurrentSettingsAsync() ?? new DeviceSettingsEntity();

                settings.UserId = userId;
                settings.Email = email;
                settings.Password = password;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedSettings = settings;

                _logger.LogInfo($"[DeviceSettings] Сессия пользователя {email} обновлена в оперативке и SQLite.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettings] Сбой сохранения сессии: {ex.Message}");
            }
        }

        public async Task ClearSessionAsync()
        {
            try
            {
                var settings = await GetCurrentSettingsAsync();
                if (settings == null) return;

                settings.UserId = Guid.Empty;
                settings.Email = string.Empty;
                settings.Password = string.Empty;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedSettings = settings;

                _logger.LogInfo("[DeviceSettings] Учетные данные устройства успешно стерты (разлогин).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettings] Ошибка очистки сессии: {ex.Message}");
            }
        }
    }
}
