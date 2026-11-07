using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Network.ApiClient;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

public class MessageApiClient : BaseApiClient, IMessageApiClient
{
    private readonly IEventBus _eventBus;

    public MessageApiClient(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus)
        : base(httpClient, logger)
    {
        _eventBus = eventBus;
    }

    public async Task<bool> PushMessageAsync(MessageEntity message)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync("/api/messages", message);

            if (response.IsSuccessStatusCode)
            {
                _eventBus.Publish(this, new IMessageApiClient.MessageSentOnServer(message));
                return true;
            }

            _eventBus.Publish(this, new IMessageApiClient.MessageSendFailed(message.Id, $"HTTP {response.StatusCode}"));
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[MessageApiClient] Ошибка отправки: {ex.Message}");
            _eventBus.Publish(this, new IMessageApiClient.MessageSendFailed(message.Id, ex.Message));
            return false;
        }
    }

    // PULL остается синхронным (для Synchronizer)
    public async Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid userId, DateTimeOffset lastSyncedAt)
    {
        try
        {
            string url = $"/api/messages/sync?userId={userId}&lastSyncedAt={Uri.EscapeDataString(lastSyncedAt.ToString("o"))}";
            var response = await HttpClient.GetFromJsonAsync<List<MessageEntity>>(url);
            return response ?? new List<MessageEntity>();
        }
        catch (Exception ex)
        {
            Logger.LogError($"[MessageApiClient] Ошибка PULL: {ex.Message}");
            return Array.Empty<MessageEntity>();
        }
    }
}