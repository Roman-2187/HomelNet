using Microsoft.AspNetCore.SignalR.Client;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network
{
    public class SignalRNotificationListener : IDisposable
    {
        private readonly string _apiBaseUrl;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus; // Наш бортовой автобус

        private readonly MessageNetworkHandler _messageHandler;
        private readonly UserNetworkHandler _userHandler;

        private HubConnection? _connection;

        public SignalRNotificationListener(
            string apiBaseUrl,
            MessageNetworkHandler messageHandler,
            UserNetworkHandler userHandler,
            ILogger logger,
            IEventBus eventBus) // Забираем шину из DI
        {
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _userHandler = userHandler ?? throw new ArgumentNullException(nameof(userHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task ConnectAsync(Guid? userId = null)
        {
            if (_connection != null) await _connection.DisposeAsync();

            string url = userId.HasValue
                ? $"{_apiBaseUrl}/chatHub?userId={userId.Value}"
                : $"{_apiBaseUrl}/chatHub";

            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            // =========================================================================
            // КРАСИВАЯ СТРОГАЯ ПОДПИСКА НА МЕТОДЫ СЕРВЕРА
            // =========================================================================

            _connection.On<MessageEntity>(_messageHandler.ServerMethodName, async (message) =>
            {
                _logger.LogInfo($"[СЕТЬ] Поймали метод '{_messageHandler.ServerMethodName}' от сервера.");
                await _messageHandler.HandleIncomingMessageAsync(message);
            });

            _connection.On<UserEntity>(_userHandler.ServerMethodName, async (user) =>
            {
                _logger.LogInfo($"[СЕТЬ] Поймали метод '{_userHandler.ServerMethodName}' от сервера.");
                await _userHandler.HandleIncomingUserAsync(user);
            });

            _connection.On<Guid>("ReceiveUserDeleted", async (userIdDeleted) =>
            {
                _logger.LogWarning("[СЕТЬ] Поймали метод 'ReceiveUserDeleted' от сервера.");
                await _userHandler.HandleIncomingUserDeletedAsync(userIdDeleted);
            });

            // 🔥 4. СЛУШАЕМ РЕАКТИВНЫЙ СИГНАЛ ГОТОВНОСТИ БЭКЕНДА
            _connection.On("ReceiveServerReady", () =>
            {
                _logger.LogInfo("[СЕТЬ] 🔥 Получен сигнал готовности от сервера! База данных на сервере прогрета.");

                // Просто шлём пакет в трубу автобуса. Никаких лямбд, чистый проброс!
                _eventBus.Publish(this, new ServerBecameReadyEvent());
                return Task.CompletedTask;
            });

            // =========================================================================

            await _connection.StartAsync();
            _logger.LogInfo(userId.HasValue
                ? $"[СЕТЬ] Постоянное соединение SignalR успешно поднято для UserId '{userId}'."
                : "[СЕТЬ] Постоянное соединение SignalR успешно поднято в режиме логирования.");
        }

        public void Dispose()
        {
        }


    }

    public record ServerBecameReadyEvent();
}
