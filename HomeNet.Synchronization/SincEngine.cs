using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization
{
    /// <summary>
    /// 🔥 ГЛАВНЫЙ СИНХРО-ДВИЖОК СИСТЕМЫ:
    /// Принимает репозитории и координатор, полностью закрывая логику фоновой репликации в ОРМ! 🦾
    /// </summary>
    public class SyncEngine
    {
        
        private readonly ISyncCoordinator _syncCoordinator;
        private readonly ICastomLogger _logger;

        public SyncEngine(
            IUserRepo userRepository,
            IMessageRepo messageRepository,
            ISyncCoordinator syncCoordinator,
            ICastomLogger logger)
        { 
            _syncCoordinator = syncCoordinator ?? throw new ArgumentNullException(nameof(syncCoordinator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Главная кнопка мессенджера: запускает репликацию!
        /// </summary>
        public async Task StartAsync()
        {
            _logger.LogInfo("=== [SiberNet Engine] ЗАПУСК СИСТЕМЫ РЕПЛИКАЦИИ ===");

            try
            {
                // Запускаем координатор, который под капотом пнет все наши синхронизаторы таблиц
                await _syncCoordinator.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SiberNet Engine Ошибка]: Не удалось запустить репликацию: {ex.Message}");
            }
        }
    }
}
