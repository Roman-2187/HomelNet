using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Extensions;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using HomeNetCore.Models; // Твои MessageEntity и UserEntity

namespace HomeNet.Network
{
    public class HttpApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpApiClient(string apiBaseUrl, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // =========================================================================
        // 📤 СУЩЕСТВУЮЩИЕ МЕТОДЫ PUSH (Оставляем как есть)
        // =========================================================================
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

        // =========================================================================
        // 📥 🔥 НАШИ НОВЫЕ МЕТОДЫ PULL (Добавляем!)
        // =========================================================================

        /// <summary>
        /// Запрашивает у сервера только те сообщения, которые появились строго ПОСЛЕ чекпоинта девайса
        /// </summary>
        public async Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid deviceId, DateTimeOffset lastSyncedAt)
        {
            try
            {
                _logger.LogInfo($"[HttpApiClient] Запрос свежих СМС для девайса {deviceId} с метки {lastSyncedAt:HH:mm:ss}...");

                // Формируем красивый URL с параметрами: /api/messages/sync?deviceId=...&lastSyncedAt=...
                // ToString("o") — это формат ISO 8601, чтобы даты с часовыми поясами передавались без искажений
                string url = $"/api/messages/sync?deviceId={deviceId}&lastSyncedAt={Uri.EscapeDataString(lastSyncedAt.ToString("o"))}";

                var response = await _httpClient.GetFromJsonAsync<List<MessageEntity>>(url);
                return response ?? new List<MessageEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HttpApiClient КРИТ] Не удалось скачать свежие сообщения: {ex.Message}");
                return Array.Empty<MessageEntity>(); // Возвращаем пустой массив, чтобы приложение не упало
            }
        }

        /// <summary>
        /// Запрашивает у сервера новые или обновленные профили пользователей
        /// </summary>
        public async Task<IReadOnlyCollection<UserEntity>> FetchFreshUsersAsync(Guid deviceId, DateTimeOffset lastSyncedAt)
        {
            try
            {
                _logger.LogInfo($"[HttpApiClient] Запрос обновлений пользователей с метки {lastSyncedAt:HH:mm:ss}...");

                string url = $"/api/users/sync?deviceId={deviceId}&lastSyncedAt={Uri.EscapeDataString(lastSyncedAt.ToString("o"))}";

                var response = await _httpClient.GetFromJsonAsync<List<UserEntity>>(url);
                return response ?? new List<UserEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[HttpApiClient КРИТ] Не удалось скачать обновления пользователей: {ex.Message}");
                return Array.Empty<UserEntity>();
            }
        }
    }
}
