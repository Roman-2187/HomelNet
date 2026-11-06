using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using System.Net.Http.Json;

namespace HomeNet.Network.Network.ApiClient
{

    public class MessageApiClient : BaseApiClient, IMessageApiClient
    {
        public MessageApiClient(HttpClient httpClient, ICastomLogger logger) : base(httpClient, logger) { }

        public async Task<bool> PushMessageAsync(MessageEntity message)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync("/api/messages", message);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageApiClient] Ошибка отправки сообщения: {ex.Message}");
                return false;
            }
        }

        public async Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid userId, DateTimeOffset lastSyncedAt)
        {
            try
            {
                Logger.LogInfo($"[MessageApiClient] Отправляем PULL по User ID: {userId}...");
                string url = $"/api/messages/sync?userId={userId}&lastSyncedAt={Uri.EscapeDataString(lastSyncedAt.ToString("o"))}";
                var response = await HttpClient.GetFromJsonAsync<List<MessageEntity>>(url);
                return response ?? new List<MessageEntity>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageApiClient] Не удалось скачать сообщения: {ex.Message}");
                return Array.Empty<MessageEntity>();
            }
        }
    }
}
