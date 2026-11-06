using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions; // 🔥 ОБЯЗАТЕЛЬНО ПОДКЛЮЧАЕМ НАШИ ЭКСТЕНШЕНЫ!
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class MessageEndpoints
    {
        public static void MapMessageEndpoints(this IEndpointRouteBuilder endpoints, IMessageRepo messageRepository, HomeNet.Infrastructure.Shared.Interfaces.Diagnostics.ICastomLogger serverLogger)
        {
            // === [📥 PULL СИНХРОНИЗАЦИЯ СМС] ===
            endpoints.MapGet("/api/messages/sync", async (Guid userId, string lastSyncedAt) =>
            {
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    if (userId == Guid.Empty) return Results.Ok(Array.Empty<MessageEntity>());

                    serverLogger.LogInfo($"[API PULL] Юзер {userId} запрашивает свои СМС с метки {clientCheckpoint:HH:mm:ss}");

                    // 🔥 ИСПРАВЛЕНО: Вызываем наш новый, очищенный от хардкода экстеншен синхронизации репозитория!
                    var freshMessages = await messageRepository.GetMessagesForSyncAsync(userId, clientCheckpoint);
                    var messageList = freshMessages.ToList();

                    serverLogger.LogInfo($"[API Успех] Отдано {messageList.Count} СМС для пользователя {userId} из PostgreSQL.");
                    return Results.Ok(messageList);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL синхронизации СМС: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 PUSH ПРИЕМ СООБЩЕНИЙ] ===
            endpoints.MapPost("/api/messages", async (MessageEntity incomingMessage, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId} для {incomingMessage.ReceiverId}");

                    // Метод остался доменным, внутри него пропишутся правильные таймстампы
                    await messageRepository.InsertAsync(incomingMessage);

                    serverLogger.LogInfo($"[API Успех] Сообщение {incomingMessage.Id} успешно записано in PostgreSQL.");

                    // Наш транслятор сетевого автобуса в действии! 🧼
                    await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString()).SendAsync("ReceiveMessage", incomingMessage);
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            endpoints.MapGet("/api/messages", () => Results.Ok(new { Status = "OK" }));
        }
    }
}
