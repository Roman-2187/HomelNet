using HomeNetCore.Interfaces;
using HomeNetCore.Extensions;
using HomeNetCore.Models.Entity;
using Microsoft.AspNetCore.SignalR;
using HomeNetOrm.Repositories.Remote;

namespace HomeNet.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints, IUserRepository userRepository, HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger)
        {
            // === [📥 PULL ОБНОВЛЕНИЯ ПРОФИЛЕЙ] ===
            endpoints.MapGet("/api/users/sync", async (Guid deviceId, string lastSyncedAt) =>
            {
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    serverLogger.LogInfo($"[API PULL] Запрос обновлений профилей через методы репозиториев...");

                    var freshUsers = await userRepository.GetUpdatesSinceAsync(clientCheckpoint);
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

                    var existingServerUser = await userRepository.GetByIdAsync(incomingUser.Id);

                    if (existingServerUser == null)
                    {
                        await userRepository.InsertUserAsync(incomingUser);
                        serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' успешно создан в PostgreSQL.");
                    }
                    else
                    {
                        serverLogger.LogInfo($"[API Чек] Пользователь '{incomingUser.Email}' найден. Обновляем поля сессии и привязываем Device ID...");

                        existingServerUser.DeviceId = incomingUser.DeviceId;
                        existingServerUser.FirstName = incomingUser.FirstName;
                        existingServerUser.LastName = incomingUser.LastName;
                        existingServerUser.PhoneNumber = incomingUser.PhoneNumber;
                        existingServerUser.UpdatedAt = DateTimeOffset.UtcNow;

                        await userRepository.UpdateAsync(existingServerUser);
                        serverLogger.LogInfo($"[API Успех] Профиль '{incomingUser.Email}' успешно обновлен в PostgreSQL. Device ID зафиксирован: {incomingUser.DeviceId}");
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
                    await userRepository.DeleteByIdAsync(id);
                    serverLogger.LogInfo($"[API Успех] Пользователь (ID: {id}) успешно удален из PostgreSQL.");

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
