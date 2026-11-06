using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalRListeners;
using HomeNetOrm.Interfaces.Repositories;
using Microsoft.AspNetCore.SignalR.Client;

public class MessageSignalRListener : BaseSignalRListener
{
    public MessageSignalRListener(string apiBaseUrl, ICastomLogger logger, IEventBus eventBus)
        : base(apiBaseUrl, logger, eventBus) { }

    protected override void RegisterHandlers(HubConnection connection)
    {
        // 🔥 Слушаем только сообщения
        connection.On<MessageEntity>("ReceiveMessageSync", (message) =>
        {
            Logger.LogInfo($"[СЕТЬ] Поймали сообщение {message.Id} от сервера.");
            message.IsSynced = 1;
            EventBus.Publish(this, new IMessageRepo.SaveRequested(message)); // Команда в репозиторий
            return Task.CompletedTask;
        });
    }
}