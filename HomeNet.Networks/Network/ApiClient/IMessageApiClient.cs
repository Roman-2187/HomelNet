using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network.Network.ApiClient
{
    public interface IMessageApiClient : IApiClientBase
    {
        Task<bool> PushMessageAsync(MessageEntity message);
        Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid userId, DateTimeOffset lastSyncedAt);
    }
}
