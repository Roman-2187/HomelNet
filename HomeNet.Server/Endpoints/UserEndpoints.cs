using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints, IUserRepo userRepository, ICastomLogger serverLogger)
        {
            // === [📥 PULL ОБНОВЛЕНИЯ ПРОФИЛЕЙ] ===
            endpoints.MapGet("/api/users/sync", async (Guid deviceId, string lastSyncedAt) =>
            {
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    serverLogger.LogInfo($"[API PULL] Запрос обновлений профилей с метки {clientCheckpoint:HH:mm:ss}...");

                    var freshUsers = await userRepository.GetUsersForSyncAsync(clientCheckpoint);
                    return Results.Ok(freshUsers.ToList());
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL ОРМ пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📥 GET ВСЕХ ПОЛЬЗОВАТЕЛЕЙ (для админки)] ===
            endpoints.MapGet("/api/users", async () =>
            {
                try
                {
                    serverLogger.LogInfo("[API] Запрос всех пользователей (админка).");
                    var users = await userRepository.GetAllAsync();
                    return Results.Ok(users);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой получения всех пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📥 GET ПОЛЬЗОВАТЕЛЯ ПО ID (для поиска)] ===
            endpoints.MapGet("/api/users/{id:guid}", async (Guid id) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API] Запрос пользователя по ID: {id}");
                    var user = await userRepository.GetByIdAsync(id);

                    if (user == null)
                        return Results.NotFound(new { Error = "Пользователь не найден" });

                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой получения пользователя по ID: {ex.Message}");
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

                    // 🔥 ПРОВЕРКА УНИКАЛЬНОСТИ EMAIL (для новых пользователей)
                    if (existingServerUser == null)
                    {
                        var userWithSameEmail = await userRepository.GetByEmailAsync(incomingUser.Email);
                        if (userWithSameEmail != null)
                        {
                            serverLogger.LogWarning($"[API Конфликт] Email '{incomingUser.Email}' уже занят.");
                            return Results.Conflict(new { Error = "Email уже зарегистрирован" });
                        }

                        await userRepository.InsertAsync(incomingUser);
                        serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' записан.");
                    }
                    else
                    {
                        serverLogger.LogInfo($"[API Чек] Пользователь найден. Обновляем профиль...");

                        existingServerUser.DeviceId = incomingUser.DeviceId;
                        existingServerUser.FirstName = incomingUser.FirstName;
                        existingServerUser.LastName = incomingUser.LastName;
                        existingServerUser.PhoneNumber = incomingUser.PhoneNumber;

                        await userRepository.UpdateAsync(existingServerUser);
                        serverLogger.LogInfo($"[API Успех] Профиль успешно обновлен.");
                    }

                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось обработать PUSH пользователя: {ex.Message}");
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
                    serverLogger.LogInfo($"[API Успех] Пользователь успешно удален.");

                    // Оповещаем всех клиентов (через SignalR)
                    await hubContext.Clients.All.SendAsync("ReceiveUserDeleted", id);

                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось удалить пользователя: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });
        }
    }
}