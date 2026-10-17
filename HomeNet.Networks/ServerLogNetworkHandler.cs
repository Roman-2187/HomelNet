using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Network;

namespace HomeNet.Network
{
    // Контракт события (убедись, что он лежит там, где его видит LogQueueManager)
    public record ServerLogReceived(string LogMessage);

    public class ServerLogNetworkHandler : INetworkHandler
    {
        private readonly IEventBus _eventBus;

        public string ServerMethodName => "ReceiveServerLog";

        public ServerLogNetworkHandler(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task HandleAsync(object payload)
        {
            if (payload == null) return;

            // 🔥 ЖЕЛЕЗОБЕТОННО: вытаскиваем чистую строку из сетевого object
            string logText = payload.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(logText))
            {
                // Сдаем посылку в наш любимый СДЭК! Передаем себя (this) как отправителя 🦾
                _eventBus.Publish(this, new ServerLogReceived(logText));
            }

            await Task.CompletedTask;
        }
    }
}
