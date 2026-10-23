using HomeNetCore.Interfaces.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using HomeNetCore.Extensions;


namespace HomeNet.Network
{
    public class SignalRNotificationListener : IDisposable
    {
        private readonly string _apiBaseUrl;
        private readonly ILogger _logger;

        // Внедряем наши конкретные двусторонние хандлеры напрямую
        private readonly MessageNetworkHandler _messageHandler;
        private readonly UserNetworkHandler _userHandler;

        private HubConnection? _connection;

        public SignalRNotificationListener(
            string apiBaseUrl,
            MessageNetworkHandler messageHandler,
            UserNetworkHandler userHandler,
            ILogger logger)
        {
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _userHandler = userHandler ?? throw new ArgumentNullException(nameof(userHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // 1. Слушаем входящие сообщения
            _connection.On<HomeNetCore.Models.Entity.MessageEntity>(_messageHandler.ServerMethodName, async (message) =>
            {
                _logger.LogInfo($"[СЕТЬ] Поймали метод '{_messageHandler.ServerMethodName}' от сервера.");
                await _messageHandler.HandleIncomingMessageAsync(message);
            });

            // 2. Слушаем входящие апдейты пользователей
            _connection.On<HomeNetCore.Models.Entity.UserEntity>(_userHandler.ServerMethodName, async (user) =>
            {
                _logger.LogInfo($"[СЕТЬ] Поймали метод '{_userHandler.ServerMethodName}' от сервера.");
                await _userHandler.HandleIncomingUserAsync(user);
            });

            // 3. Слушаем входящие удаления пользователей
            _connection.On<Guid>("ReceiveUserDeleted", async (userIdDeleted) =>
            {
                _logger.LogWarning("[СЕТЬ] Поймали метод 'ReceiveUserDeleted' от сервера.");
                await _userHandler.HandleIncomingUserDeletedAsync(userIdDeleted);
            });

            // =========================================================================

            await _connection.StartAsync();
            _logger.LogInfo(userId.HasValue
                ? $"[СЕТЬ] Постоянное соединение SignalR успешно поднято для UserId '{userId}'."
                : "[СЕТЬ] Постоянное соединение SignalR успешно поднято в режиме логирования.");
        }

        public void Dispose()
        {
            // Здесь можно неявно почистить ресурсы, если нужно, 
            // но хандлеры живут в DI как синглтоны, так что пускай работают.
        }
    }
}
