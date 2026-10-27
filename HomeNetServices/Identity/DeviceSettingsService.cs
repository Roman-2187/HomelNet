using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Repositories.Local;
using HomeNetCore.Extensions;
using System.Threading;

namespace HomeNetServices.Identity
{
    public class DeviceSettingsService : IDeviceSettingsService
    {
        private readonly ClientDeviceSettingsRepository _deviceSettingsRepo;
        private readonly ILogger _logger;

        // 🔥 Наш вечный стейт в оперативке
        private DeviceSettingsEntity? _cachedSettings;

        // Смарт-замок для предотвращения race condition при ленивой инициализации
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DeviceSettingsService(ClientDeviceSettingsRepository deviceSettingsRepo, ILogger logger)
        {
            _deviceSettingsRepo = deviceSettingsRepo ?? throw new ArgumentNullException(nameof(deviceSettingsRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // ЧИСТОТА: Конструктор пустой и стерильный! Никаких .Wait() и Task.Run.
        }

        /// <summary>
        /// Потокобезопасный ленивый прогрев кэша. Запустится только тогда, 
        /// когда реально понадобится ID или настройки, и базы уже точно будут готовы.
        /// </summary>
        private async Task EnsureSettingsLoadedAsync()
        {
            if (_cachedSettings != null) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_cachedSettings == null)
                {
                    _logger.LogInfo("[DeviceSettingsService] Ленивый прогрев настроек железа...");
                    // Базы к этому моменту уже гарантированно запущены
                    Guid deviceId = await _deviceSettingsRepo.GetOrCreateDeviceIdAsync();
                    _cachedSettings = await _deviceSettingsRepo.GetSettingsAsync();
                    _logger.LogInfo($"[DeviceSettingsService] Сервер настроек прогрет в рантайме. ID: {deviceId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettingsService] Ошибка ленивой инициализации настроек: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Guid> GetDeviceIdAsync()
        {
            await EnsureSettingsLoadedAsync();
            return _cachedSettings?.DeviceId ?? Guid.Empty;
        }

        public async Task<DeviceSettingsEntity?> GetCurrentSettingsAsync()
        {
            await EnsureSettingsLoadedAsync();
            return _cachedSettings;
        }

        public async Task SaveSessionAsync(Guid userId, string email, string password)
        {
            try
            {
                await EnsureSettingsLoadedAsync();
                var settings = _cachedSettings ?? new DeviceSettingsEntity();

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
                // 1. Гарантируем, что кэш прогрет (уже после инициализации СУБД!)
                await EnsureSettingsLoadedAsync();

                var settings = _cachedSettings;
                if (settings == null) return;

                // 2. Зануляем поля сессии прямо в объекте
                settings.UserId = Guid.Empty;
                settings.Email = string.Empty;
                settings.Password = string.Empty;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                // 3. Вызываем НАШ РОДНОЙ UPSERT генератор! 
                // Теперь он отработает в штатном режиме, поймает конфликт по DeviceId 
                // и запишет пустые поля из EXCLUDED прямо в SQLite!
                await _deviceSettingsRepo.SaveSettingsAsync(settings);

                // 4. Обновляем оперативку
                _cachedSettings = settings;

                _logger.LogInfo("[DeviceSettings] Учетные данные устройства успешно стерты через UPSERT-генератор (разлогин).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettings] Ошибка очистки сессии через генератор: {ex.Message}");
            }
        }

    }
}
