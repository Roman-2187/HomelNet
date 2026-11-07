using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
using Microsoft.AspNetCore.SignalR;
using System.Data.Common;

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


            // === [🔐 АУТЕНТИФИКАЦИЯ] ===
            endpoints.MapPost("/api/users/auth", async (LoginRequest request) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API AUTH] Попытка входа для {request.Email}...");

                    // Ищем юзера по Email в Postgres
                    var user = await userRepository.GetByEmailAsync(request.Email);

                    if (user == null || user.Password != request.Password)
                    {
                        serverLogger.LogWarning($"[API AUTH] Неверный email или пароль для {request.Email}");
                        return Results.Unauthorized(); // Вернет 401
                    }

                    serverLogger.LogInfo($"[API AUTH] Успешный вход для {request.Email}");
                    return Results.Ok(user); // Вернет 200 и JSON с юзером
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API AUTH] Ошибка авторизации: {ex.Message}");
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

                    // 🔥 Ищем по ID (это единственный правильный способ проверить, есть ли такой юзер)
                    var existingServerUser = await userRepository.GetByIdAsync(incomingUser.Id);

                    if (existingServerUser != null)
                    {
                        // Юзер существует по ID — обновляем (это нормальный PUSH)
                        serverLogger.LogInfo($"[API Чек] Пользователь найден по ID. Обновляем профиль...");

                        existingServerUser.DeviceId = incomingUser.DeviceId;
                        existingServerUser.FirstName = incomingUser.FirstName;
                        existingServerUser.LastName = incomingUser.LastName;
                        existingServerUser.PhoneNumber = incomingUser.PhoneNumber;

                        await userRepository.UpdateAsync(existingServerUser);
                        serverLogger.LogInfo($"[API Успех] Профиль успешно обновлен.");
                    }
                    else
                    {
                        // Юзера по ID нет — это новый. Пытаемся вставить.
                        // Если Email занят — репозиторий кинет DuplicateEmailException.
                        serverLogger.LogInfo($"[API Новый] Юзера нет по ID. Пытаемся создать...");
                        await userRepository.InsertAsync(incomingUser);
                        serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' записан.");
                    }

                    return Results.Ok(new { Success = true });
                }
                catch (DuplicateEmailException ex) // 🔥 Ловим доменное исключение от репозитория!
                {
                    serverLogger.LogWarning($"[API Конфликт] {ex.Message}");
                    return Results.Conflict(new { Error = ex.Message });
                }
                catch (DatabaseException ex) // 🔥 Ловим ошибку БД (упала база, синтаксис и т.д.)
                {
                    serverLogger.LogError($"[API Ошибка БД] {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось обработать PUSH: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });
        }
        public record LoginRequest(string Email, string Password);
    }
}