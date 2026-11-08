using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Exeptions; // Для DuplicateMessageException
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization.Sinc
{
    public class MessageSyncService : ISyncCoordinator, IDisposable
    {
        private readonly IMessageRepo _messageRepository;
        private readonly IMessageApiClient _apiClient;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;

        private Guid _currentUserId;
        private bool _isSyncing = false;

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

        public Task StartAsync()
        {
            _logger.LogInfo("[MessageSyncService] Ожидание авторизации для старта синхронизации...");
            return Task.CompletedTask;
        }

        private async void OnUserAuthenticated(Guid userId)
        {
            if (_isSyncing) return;
            _currentUserId = userId;
            _isSyncing = true;

            _logger.LogInfo($"[MessageSyncService] Пользователь авторизован ({_currentUserId}). Начинаем синхронизацию.");

            await PullAsync();
            await PushAsync();

            _logger.LogInfo("[MessageSyncService] Синхронизация завершена.");
        }

        private async Task PullAsync()
        {
            try
            {
                var freshMessages = await _apiClient.FetchFreshMessagesAsync(_currentUserId, DateTimeOffset.MinValue);
                foreach (var message in freshMessages)
                {
                    message.IsSynced = 1;
                    await TryInsertMessageAsync(message); // 🔥 Используем скромный метод
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

        // 🔥 СКРОМНЫЙ МЕТОД ДЛЯ ВСТАВКИ
        private async Task TryInsertMessageAsync(MessageEntity message)
        {
            try
            {
                await _messageRepository.InsertAsync(message);
            }
            catch (DuplicateMessageException)
            {
                _logger.LogDebug($"  сообщение {message.Id} пропущено по дубликату ");
            }
            catch (Exception ex)
            {
                _logger.LogError($" Не удалось сохранить сообщение {message.Id}: {ex.Message}");
            }
        }

        private void SubscribeEvents()
        {
            _eventBus.Subscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);
            _eventBus.Subscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);

            _eventBus.Subscribe<IUserVm.UserSignedIn>(msg => {
                if (msg?.User != null) OnUserAuthenticated(msg.User.Id);
            });

            _eventBus.Subscribe<IUserVm.UserSignedUp>(msg => {
                if (msg?.User != null) OnUserAuthenticated(msg.User.Id);
            });
        }

        private async void OnMessageSent(IMessageApiClient.MessageSentOnServer msg)
        {
            _logger.LogInfo($"[MessageSyncService] Сервер подтвердил сообщение {msg.Message.Id}. Пишем в SQLite.");
            await TryInsertMessageAsync(msg.Message); // 🔥 Используем тот же скромный метод
        }

        private async void OnMessageReceivedFromNetwork(MessageReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[MessageSyncService] Сеть прислала сообщение {msg.Message.Id}. Пишем в SQLite.");
            await TryInsertMessageAsync(msg.Message); // 🔥 Используем тот же скромный метод
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);
            _eventBus.Unsubscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }
    }
}