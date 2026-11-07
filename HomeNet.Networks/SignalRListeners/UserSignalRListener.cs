using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalRListeners;
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

            // 🔥 Публикуем СВОЙ факт (от имени листенера)
            EventBus.Publish(this, new UserReceivedFromNetwork(user));
            return Task.CompletedTask;
        });

        connection.On<Guid>("ReceiveUserDeleted", (userId) =>
        {
            Logger.LogWarning($"[СЕТЬ] Поймали удаление юзера {userId}.");

            // 🔥 Публикуем СВОЙ факт
            EventBus.Publish(this, new UserDeletedFromNetwork(userId));
            return Task.CompletedTask;
        });
    }
}

// 🔥 Новые факты для сети
public record UserReceivedFromNetwork(UserEntity User);
public record UserDeletedFromNetwork(Guid UserId);