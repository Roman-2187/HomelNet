using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using System.Net.Http.Json;

namespace HomeNet.Network.Network.ApiClient
{



    public class UserApiClient : BaseApiClient, IUserApiClient
    {
        public UserApiClient(HttpClient httpClient, ICastomLogger logger) : base(httpClient, logger) { }

        public async Task<bool> PushUserAsync(UserEntity user)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync("/api/users", user);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка отправки пользователя: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"/api/users/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка удаления пользователя: {ex.Message}");
                return false;
            }
        }

        public async Task<IReadOnlyCollection<UserEntity>> FetchFreshUsersAsync(Guid deviceId, DateTimeOffset lastSyncedAt)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Запрос обновлений пользователей с метки {lastSyncedAt:HH:mm:ss}...");
                string url = $"/api/users/sync?deviceId={deviceId}&lastSyncedAt={Uri.EscapeDataString(lastSyncedAt.ToString("o"))}";
                var response = await HttpClient.GetFromJsonAsync<List<UserEntity>>(url);
                return response ?? new List<UserEntity>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Не удалось скачать обновления пользователей: {ex.Message}");
                return Array.Empty<UserEntity>();
            }
        }
    }
}
