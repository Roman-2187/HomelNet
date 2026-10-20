using HomeNet.DI.Modules;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Repositories;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server
{
    public partial class Program
    {
        private static readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger = Logger.CreateConsoleLogger();

           

            var schemaModule = new RemoteMessengerSchemaModule();
            var serverSchemas = schemaModule.GetSchemas();

            string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
                ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

            var postgresEngine = new SincedPostgresDbEngine(postgresConn, serverSchemas, serverLogger);

            IMessageRepository messageRepository = postgresEngine.CreateMessageRepository();
            IUserRepository userRepository = postgresEngine.CreateUserRepository();

            // 🔥 ПОЛУЧАЕМ ПУБЛИЧНЫЙ РЕПОЗИТОРИЙ СЕССИЙ ДЕВАЙСОВ:
            IDeviceSyncRepository deviceSyncRepository = postgresEngine.CreateDeviceSyncRepository();

            builder.Services.AddSignalR();
            var app = builder.Build();

            app.MapHub<ServerChatHub>("/chatHub");

            // ============================================================================
            // 📥 🎯 ЭНДПОИНТ PULL: СИНХРОНИЗАЦИЯ СМС ДЛЯ КЛИЕНТА (Строго через методы ОРМ)
            // ============================================================================
            app.MapGet("/api/messages/sync", async (Guid deviceId, string lastSyncedAt) =>
            {
                await _dbSemaphore.WaitAsync();
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    // 1. Вызываем метод расширения репозитория девайсов, чтобы узнать UserId
                    // (Репозиторий сам под капотом сделает SELECT к device_sync через internal генератор)
                    var deviceRepo = deviceSyncRepository as IDeviceSyncRepository;
                    Guid userId = deviceRepo != null ? await deviceRepo.GetUserIdByDeviceAsync(deviceId) : Guid.Empty;

                    if (userId == Guid.Empty) return Results.Ok(Array.Empty<MessageEntity>());

                    serverLogger.LogInfo($"[API PULL] Девайс {deviceId} (Юзер: {userId}) запрашивает СМС с метки {clientCheckpoint:HH:mm:ss}");

                    // 2. 🔥 ЧИСТЫЙ ВЫЗОВ: Берем и вызываем наш универсальный экстеншен на репозитории сообщений!
                    var freshMessages = await messageRepository.GetFreshMessagesForUserAsync(userId, clientCheckpoint);
                    var messageList = freshMessages.ToList();

                    // 3. 🔥 ЧИСТЫЙ UPSERT: Сдвигаем чекпоинт времени девайса в Postgres через универсальный экстеншен!
                    await deviceSyncRepository.UpdateDeviceCheckpointAsync(deviceId, userId, DateTimeOffset.UtcNow);

                    serverLogger.LogInfo($"[API Успех] Отдано {messageList.Count} СМС для девайса {deviceId} через методы ОРМ.");
                    return Results.Ok(messageList);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL синхронизации СМС через методы репозиториев: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            });

            // ============================================================================
            // 📥 🎯 ЭНДПОИНТ PULL: ВЫДАЧА ОБНОВЛЕНИЙ ПОЛЬЗОВАТЕЛЕЙ (Строго через методы ОРМ)
            // ============================================================================
            app.MapGet("/api/users/sync", async (Guid deviceId, string lastSyncedAt) =>
            {
                await _dbSemaphore.WaitAsync();
                try
                {
                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    serverLogger.LogInfo($"[API PULL] Запрос обновлений профилей через методы репозиториев...");

                    // 🔥 ЧИСТЫЙ ВЫЗОВ: Наш универсальный экстеншен сам выгребет свежих юзеров по таймстемпу!
                    var freshUsers = await userRepository.GetUpdatesSinceAsync(clientCheckpoint);

                    return Results.Ok(freshUsers.ToList());
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL ОРМ пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            });

            // ============================================================================
            // 📤 ЭНДПОИНТ: ПРИЕМ СООБЩЕНИЙ ЧЕРЕЗ РЕПОЗИТОРИЙ (Твой старый метод PUSH)
            // ============================================================================
            app.MapPost("/api/messages", async (MessageEntity incomingMessage, IHubContext<ServerChatHub> hubContext) =>
            {
                await _dbSemaphore.WaitAsync();
                try
                {
                    serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId} для {incomingMessage.ReceiverId}");
                    await messageRepository.SaveMessageAsync(incomingMessage);
                    serverLogger.LogInfo($"[API Успех] Сообщение {incomingMessage.Id} успешно записано в PostgreSQL.");

                    await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString()).SendAsync("ReceiveMessage", incomingMessage);
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            });

            // ============================================================================
            // 📤 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ (Твой старый метод PUSH)
            // ============================================================================
            app.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
            {
                await _dbSemaphore.WaitAsync();
                try
                {
                    serverLogger.LogInfo($"[API] Прилетел профиль пользователя '{incomingUser.FirstName}' (ID: {incomingUser.Id})");
                    await userRepository.InsertUserAsync(incomingUser);
                    serverLogger.LogInfo($"[API Успех] Пользователь '{incomingUser.FirstName}' (ID: {incomingUser.Id}) успешно сохранен в PostgreSQL.");
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось сохранить пользователя через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            });

            app.MapGet("/api/ping", () => Results.Ok("pong"));

            // ============================================================================
            // 📤 ЭНДПОИНТ: УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ (Твой старый метод)
            // ============================================================================
            app.MapDelete("/api/users/{id:guid}", async (Guid id, IHubContext<ServerChatHub> hubContext) =>
            {
                await _dbSemaphore.WaitAsync();
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
                finally
                {
                    _dbSemaphore.Release();
                }
            });

            // ============================================================================
            // 📦 ИНИЦИАЛИЗАЦИЯ И ЗАПУСК
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