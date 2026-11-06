using HomeNet.Models.Shared.Entity; 
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions; 
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints, IUserRepo userRepository, HomeNet.Infrastructure.Shared.Interfaces.Diagnostics.ICastomLogger serverLogger)
        {
            // === [📥 PULL ОБНОВЛЕНИЯ ПРОФИЛЕЙ] ===
            endpoints.MapGet("/api/users/sync", async (Guid deviceId, string lastSyncedAt) =>
            {
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    serverLogger.LogInfo($"[API PULL] Запрос обновлений профилей через методы репозиториев...");

                    // 🔥 ИСПРАВЛЕНО: Вызываем наш новый симметричный экстеншен репликации
                    var freshUsers = await userRepository.GetUsersForSyncAsync(clientCheckpoint);
                    return Results.Ok(freshUsers.ToList());
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL ОРМ пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 PUSH РЕГИСТРАЦИЯ И БИЗНЕС-UPSERT] ===
            endpoints.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API PUSH] Прилетел профиль пользователя '{incomingUser.Email}' (ID: {incomingUser.Id})");

                    // 🔥 ИСПРАВЛЕНО: Вызываем GetByIdAsync через глобальный дженерик-обвес
                    var existingServerUser = await userRepository.GetByIdAsync(incomingUser.Id);

                    if (existingServerUser == null)
                    {
                        await userRepository.InsertAsync(incomingUser);
                        serverLogger.LogInfo($"[API Успех] Новый... записан.");
                    }
                    else
                    {
                        serverLogger.LogInfo($"[API Чек] Пользователь... найден. Обновляем сессию...");

                        existingServerUser.DeviceId = incomingUser.DeviceId;
                        existingServerUser.FirstName = incomingUser.FirstName;
                        existingServerUser.LastName = incomingUser.LastName;
                        existingServerUser.PhoneNumber = incomingUser.PhoneNumber;

                        // 🔥 ИСПРАВЛЕНО: Вызываем UpdateAsync через глобальный дженерик-обвес
                        await userRepository.UpdateAsync(existingServerUser);
                        serverLogger.LogInfo($"[API Успех] Профиль... успешно обновлен.");
                    }

                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось обработать PUSH пользователя через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ] ===
            endpoints.MapDelete("/api/users/{id:guid}", async (Guid id, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API] Запрос на удаление пользователя (ID: {id})");

                    // 🔥 ИСПРАВЛЕНО: Вызываем DeleteByIdAsync через глобальный дженерик-обвес
                    await userRepository.DeleteByIdAsync(id);
                    serverLogger.LogInfo($"[API Успех] Пользователь успешно удален.");

                    await hubContext.Clients.All.SendAsync("ReceiveUserDeleted", id);
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось удалить пользователя через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });
        }
    }
}
