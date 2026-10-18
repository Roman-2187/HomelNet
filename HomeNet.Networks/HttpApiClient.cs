using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Extensions;
using System.Net.Http.Json;

namespace HomeNet.Network
{
    public class HttpApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpApiClient(string apiBaseUrl, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Инициализируем стандартный HTTP-клиент с адресом нашего ASP.NET API
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10) // Таймаут, чтобы приложение не зависло навечно при потере сети
            };
        }

        public async Task<bool> PushMessageAsync(object messageEntity)
        {
            try
            {          
                var response = await _httpClient.PostAsJsonAsync("/api/messages", messageEntity);
               
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HttpApiClient] Ошибка отправки сообщения по сети: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PushUserAsync(object userEntity)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/users", userEntity);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HttpApiClient] Ошибка отправки пользователя по сети: {ex.Message}");
                return false;
            }
        }

      
        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {           
                var response = await _httpClient.DeleteAsync($"/api/users/{userId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HttpApiClient] Ошибка при отправке запроса на удаление (ID: {userId}): {ex.Message}");
                return false;
            }
        }

   
        public bool IsServerAvailable
        {
            get
            {
                try
                {                
                    var response = _httpClient.GetAsync("api/ping").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;                
                }
                catch
                {                
                    return false;
                }
            }
        }

    }
}
