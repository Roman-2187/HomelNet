using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Network;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;



namespace HomeNet.Network
{
    public class SignalRNotificationListener
    {
        private readonly string _apiBaseUrl;
        private readonly IEnumerable<INetworkHandler> _handlers;
        private readonly ILogger _logger;
        private Microsoft.AspNetCore.SignalR.Client.HubConnection? _connection;



        // Принимает коллекцию обработчиков (массив из MessageNetworkHandler, UserNetworkHandler и т.д.)
        public SignalRNotificationListener(string apiBaseUrl, IEnumerable<INetworkHandler> handlers, ILogger logger)
        {
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConnectAsync(Guid userId)
        {
            if (_connection != null) await _connection.DisposeAsync();

            _connection = new HubConnectionBuilder()
                .WithUrl($"{_apiBaseUrl}/chatHub?userId={userId}")
                .WithAutomaticReconnect()
                .Build();
            // 🔥 КРАСОТА АРХИТЕКТУРЫ: Регистрируем обработчики на хабе сервера!
            foreach (var handler in _handlers)
            {
                // Используем встроенную generic-перегрузку SignalR, которая принимает object
                _connection.On<object>(handler.ServerMethodName, async (payload) =>
                {
                    _logger.LogInfo($"[СЕТЬ] Поймали сетевое событие '{handler.ServerMethodName}' от сервера.");

                    if (payload != null)
                    {
                        await handler.HandleAsync(payload);
                    }
                });
            }


            await _connection.StartAsync();
            _logger.LogInfo($"[СЕТЬ] Постоянное соединение SignalR успешно поднято для UserId '{userId}'.");
        }
    }
}
