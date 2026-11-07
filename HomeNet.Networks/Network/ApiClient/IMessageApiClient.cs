using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Network.ApiClient;

public interface IMessageApiClient : IApiClientBase
{
    Task<bool> PushMessageAsync(MessageEntity message);
    Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid userId, DateTimeOffset lastSyncedAt);

    // 🔥 ФАКТЫ (публикует сам ApiClient)
    public record MessageSentOnServer(MessageEntity Message);
    public record MessageSendFailed(Guid MessageId, string Error);
}