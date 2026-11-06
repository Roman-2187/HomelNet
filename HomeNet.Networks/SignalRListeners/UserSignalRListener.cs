using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalRListeners;
using HomeNetOrm.Interfaces.Repositories;
using Microsoft.AspNetCore.SignalR.Client;

public class UserSignalRListener : BaseSignalRListener
{
    public UserSignalRListener(string apiBaseUrl, ICastomLogger logger, IEventBus eventBus)
        : base(apiBaseUrl, logger, eventBus) { }

    protected override void RegisterHandlers(HubConnection connection)
    {
        // 🔥 Слушаем только юзеров
        connection.On<UserEntity>("ReceiveUserSync", (user) =>
        {
            Logger.LogInfo($"[СЕТЬ] Поймали юзера {user.Id} от сервера.");
            user.IsSynced = 1;
            EventBus.Publish(this, new IUserRepo.InsertRequested(user)); // Команда в репозиторий
            return Task.CompletedTask;
        });

        connection.On<Guid>("ReceiveUserDeleted", (userId) =>
        {
            Logger.LogWarning($"[СЕТЬ] Поймали удаление юзера {userId}.");
            EventBus.Publish(this, new IUserRepo.DeleteRequested(userId));
            return Task.CompletedTask;
        });
    }
}