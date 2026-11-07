using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization.Sinc
{
    public class MessageSyncService : ISyncCoordinator, IDisposable
    {
        private readonly IMessageRepo _messageRepository;
        private readonly IMessageApiClient _apiClient;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;
        private bool _isInitialSyncExecuted;

        public MessageSyncService(
            IMessageRepo messageRepository,
            IMessageApiClient apiClient,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        // =========================================================================
        // 📥 СТАРТОВАЯ СИНХРОНИЗАЦИЯ (PULL/PUSH)
        // =========================================================================
        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            _logger.LogInfo("=== [MessageSyncService] ЗАПУСК СТАРТОВОЙ СИНХРОНИЗАЦИИ ===");

            await PullAsync();
            await PushAsync();

            _logger.LogInfo("=== [MessageSyncService] СТАРТОВАЯ СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА ===");
        }

        private async Task PullAsync()
        {
            try
            {
                // В реальном коде userId берется из сессии
                var freshMessages = await _apiClient.FetchFreshMessagesAsync(Guid.Empty, DateTimeOffset.MinValue);
                foreach (var message in freshMessages)
                {
                    message.IsSynced = 1;
                    await _messageRepository.InsertAsync(message); // Репозиторий крикнет Saved
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncService] Ошибка PULL: {ex.Message}");
            }
        }

        private async Task PushAsync()
        {
            try
            {
                var allMessages = await _messageRepository.GetAllAsync();
                var pending = allMessages.Where(m => m.IsSynced == 0).ToList();

                if (pending.Count > 0)
                {
                    _logger.LogWarning($"[MessageSyncService] Обнаружено {pending.Count} неотправленных СМС. Инициируем PUSH.");
                    foreach (var message in pending)
                    {
                        await _apiClient.PushMessageAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncService] Ошибка PUSH: {ex.Message}");
            }
        }

        // =========================================================================
        // 🎧 РЕАКЦИЯ НА СОБЫТИЯ
        // =========================================================================
        private void SubscribeEvents()
        {
            // Факты от API (успешная отправка на сервер)
            _eventBus.Subscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);

            // Факты от SignalR (сеть прислала сообщение)
            _eventBus.Subscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }

        private async void OnMessageSent(IMessageApiClient.MessageSentOnServer msg)
        {
            _logger.LogInfo($"[MessageSyncService] Сервер подтвердил сообщение {msg.Message.Id}. Пишем в SQLite.");
            await _messageRepository.InsertAsync(msg.Message);
        }

        // 🔥 Обработчик для сети
        private async void OnMessageReceivedFromNetwork(MessageReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[MessageSyncService] Сеть прислала сообщение {msg.Message.Id}. Пишем в SQLite.");
            await _messageRepository.InsertAsync(msg.Message);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);
            _eventBus.Unsubscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }
    }
}