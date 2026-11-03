using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Interfaces.Builders;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeNetOrm.Infrastructure
{
    public class DbContextContainer : IDbContextContainer
    {
        private readonly IDbEngine _localEngine;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;
        private readonly string _remoteServerUrl;
        private readonly HttpClient _pingClient;

        // Чистая абстракция состояний подключения
        public bool IsLocalAvailable { get; private set; }
        public bool IsRemoteAvailable { get; private set; }

        // Связываем интерфейсные свойства напрямую с переменными
        public bool IsServerAvailable => IsRemoteAvailable;
        public bool IsPostgresAvailable => IsRemoteAvailable;

        // Конструктор принимает строго локальный абстрактный движок и адрес сервера
        public DbContextContainer(
            IDbEngine localEngine,
            string remoteServerUrl,
            IEventBus eventBus,
            ILogger logger)
        {
            _localEngine = localEngine ?? throw new ArgumentNullException(nameof(localEngine));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _remoteServerUrl = remoteServerUrl ?? throw new ArgumentNullException(nameof(remoteServerUrl));

            _pingClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        }

        public async Task InitializeAllDatabasesAsync()
        {
            _logger.LogInfo("=== [СУБД ХАБ] ЗАПУСК ИНИЦИАЛИЗАЦИИ ХРАНИЛИЩ ===");

            // Запускаем локальный движок данных и проверяем сервер последовательно
            await InitializeLocalChannelInternalAsync();
            await CheckRemoteServerAvailabilityInternalAsync();

            _logger.LogInfo("=== [СУБД ХАБ] ИНИЦИАЛИЗАЦИЯ ХРАНИЛИЩ ЗАВЕРШЕНА ===");
            _eventBus.Publish(this, new IDbContextContainer.InfrastructureReady());
        }

        private async Task InitializeLocalChannelInternalAsync()
        {
            try
            {
                await _localEngine.InitializeChannelAsync();
                IsLocalAvailable = true;
                _logger.LogInfo("[СУБД СТАТУС] Локальный движок данных успешно запущен.");
            }
            catch (Exception ex)
            {
                IsLocalAvailable = false;
                _logger.LogError($"[СУБД СТАТУС] Локальный движок недоступен: {ex.Message}");
            }
        }

        private async Task CheckRemoteServerAvailabilityInternalAsync()
        {
            try
            {
                // Проверяем доступность удаленного API сервера
                var response = await _pingClient.GetAsync($"{_remoteServerUrl}/api/messages");
                IsRemoteAvailable = true;
                _logger.LogInfo($"[СЕТЬ СТАТУС] Удаленный сервер успешно обнаружен по адресу: {_remoteServerUrl}");
            }
            catch (Exception ex)
            {
                IsRemoteAvailable = false;
                _logger.LogError($"[СЕТЬ СТАТУС] Удаленный сервер недоступен: {ex.Message}");
            }
        }
    }
}
