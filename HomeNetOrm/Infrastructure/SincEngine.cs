
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc; // Для ISyncCoordinator

namespace HomeNetOrm.Infrastructure
{
    /// <summary>
    /// 🔥 ГЛАВНЫЙ СИНХРО-ДВИЖОК СИСТЕМЫ:
    /// Принимает репозитории и координатор, полностью закрывая логику фоновой репликации в ОРМ! 🦾
    /// </summary>
    public class SincEngine
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly ISyncCoordinator _syncCoordinator;
        private readonly ILogger _logger;

        public SincEngine(
            IUserRepository userRepository,
            IMessageRepository messageRepository,
            ISyncCoordinator syncCoordinator,
            ILogger logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _syncCoordinator = syncCoordinator ?? throw new ArgumentNullException(nameof(syncCoordinator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Главная кнопка мессенджера: запускает пакетную сверку данных!
        /// </summary>
        public async Task StartAsync()
        {
            _logger.LogInfo("=== [SiberNet Engine] ЗАПУСК СИСТЕМЫ СИНХРОНИЗАЦИИ ===");

            try
            {
                // Запускаем координатор, который под капотом параллельно пнет все наши синхронизаторы таблиц
                await _syncCoordinator.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SiberNet Engine Ошибка]: Не удалось запустить репликацию: {ex.Message}");
            }
        }
    }
}

