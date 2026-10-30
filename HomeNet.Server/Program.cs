using HomeNet.DI.Modules;
using HomeNet.DI.Modules;
using HomeNetCore.Extensions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Repositories.Remote;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace HomeNet.Server
{
    public partial class Program
    {
        // 🔥 ФИКС: УДАЛЕН ГЛОБАЛЬНЫЙ МЕРТВЫЙ ЗАМОК _dbSemaphore! База теперь работает в параллели.

        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            const string serverLogName = "server_debug";

            HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger = new Logger((logEntry, level, detectedNamespace) =>
            {
                DiagnosticHub.Log(logEntry, (HomeNetCore.Enums.LogLevel)level, serverLogName);
            });

            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] СИСТЕМА ДВОЙНОГО ЛОГИРОВАНИЯ ИНИЦИАЛИЗИРОВАНА ===");

            var schemaModule = new RemoteMessengerSchemaModule();
            var serverSchemas = schemaModule.GetSchemas();

            string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
                ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

            // 🎯 ЗАПУСКАЕМ СЕРВЕРНУЮ ЦАРЬ-МАШИНУ: Режим RemoteServer на полную мощность!
            var postgresEngine = new SiberNetDbEngine(DbEngineType.RemoteServer, postgresConn, serverSchemas, serverLogger);

            // Штампуем репозитории на полном автомате по единому конвейеру.
            IMessageRepository messageRepository = postgresEngine.CreateMessageRepository(null!);
            IUserRepository userRepository = postgresEngine.CreateUserRepository(null!);
            IDeviceSyncRepository deviceSyncRepository = postgresEngine.CreateDeviceSyncRepository();

            builder.Services.AddSignalR();
            var app = builder.Build();

            app.MapHub<ServerChatHub>("/chatHub");

            // ============================================================================
            // 📥 🎯 ЭНДПОИНТ PULL: СИНХРОНИЗАЦИЯ СМС ПО USER ID (Асинхронный и параллельный)
            // ============================================================================
            app.MapGet("/api/messages/sync", async (Guid userId, string lastSyncedAt) =>
            {
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    if (userId == Guid.Empty) return Results.Ok(Array.Empty<MessageEntity>());

                    serverLogger.LogInfo($"[API PULL] Юзер {userId} запрашивает свои СМС с метки {clientCheckpoint:HH:mm:ss}");

                    var freshMessages = await messageRepository.GetFreshMessagesForUserAsync(userId, clientCheckpoint);
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

            // ============================================================================
            // 📥 🎯 ЭНДПОИНТ PULL: ВЫДАЧА ОБНОВЛЕНИЙ ПОЛЬЗОВАТЕЛЕЙ (Без блокировок)
            // ============================================================================
            app.MapGet("/api/users/sync", async (Guid deviceId, string lastSyncedAt) =>
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

            // ============================================================================
            // 📤 ЭНДПОИНТ: ПРИЕМ СООБЩЕНИЙ ЧЕРЕЗ РЕПОЗИТОРИЙ (PUSH)
            // ============================================================================
            app.MapPost("/api/messages", async (MessageEntity incomingMessage, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId} для {incomingMessage.ReceiverId}");
                    await messageRepository.SaveMessageAsync(incomingMessage);
                    serverLogger.LogInfo($"[API Успех] Сообщение {incomingMessage.Id} успешно записано in PostgreSQL.");

                    await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString()).SendAsync("ReceiveMessage", incomingMessage);
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // ============================================================================
            // 📤 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ И СИНХРОНИЗАЦИИ (БИЗНЕС-UPSERT)
            // ============================================================================
            app.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
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

            // ============================================================================
            // 📤 ЭНДПОИНТ: УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ
            // ============================================================================
            app.MapDelete("/api/users/{id:guid}", async (Guid id, IHubContext<ServerChatHub> hubContext) =>
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

            // ============================================================================
            // 📦 ИНИЦИАЛИЗАЦИЯ И ЗАПУСК БАЗЫ ДАННЫХ СЕРВЕРА
            // ============================================================================
            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");
            await postgresEngine.InitializeChannelAsync();
            serverLogger.LogInfo("[SERVER START] Веб-сервер запущен и слушает порт.");
            app.Run();
        }
    }


public class ServerChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            string? userId = Context.GetHttpContext()?.Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
    }
}