using HomeNetCore.Interfaces; // Убедись, что тут лежит твой интерфейс IApiClient
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Extensions;
using System.Net.Http.Json; // Нужен для удобной отправки объектов как JSON

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
                // Метод .PostAsJsonAsync сам берет любой C# объект, 
                // делает из него JSON-строку и шлет на сервер
                var response = await _httpClient.PostAsJsonAsync("/api/messages", messageEntity);

                // Возвращаем true, если сервер ответил 200 OK
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


        // 🔥 РЕАЛИЗАЦИЯ СВОЙСТВА ПРОВЕРКИ ДОСТУПНОСТИ СЕРВЕРА
        public bool IsServerAvailable
        {
            get
            {
                try
                {
                    // Стучимся на выделенный эндпоинт проверки связи
                    var response = _httpClient.GetAsync("api/ping").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;


                    
                }
                catch
                {
                    // Если легли по таймауту или нет сети — сервер недоступен
                    return false;
                }
            }
        }

    }
}
