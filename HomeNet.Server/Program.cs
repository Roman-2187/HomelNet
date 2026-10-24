using HomeNet.DI.Modules;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models.Entity;
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

            // 🎯 1. Настраиваем гарантированный путь к файлу на Рабочем столе
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string debugFolderPath = Path.Combine(desktopPath, "debug");

            if (!Directory.Exists(debugFolderPath))
            {
                Directory.CreateDirectory(debugFolderPath);
            }

            string serverLogPath = Path.Combine(debugFolderPath, "server_debug.txt");
            AppFileLogger fileLogger = new AppFileLogger(serverLogPath);


            // 🎯 2. Объединяем вывод в КОНСОЛЬ и ФАЙЛ внутри одного общего логгера!
            HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger = new Logger((logEntry, level, detectedNamespace) =>
            {
                // Пишем цветной лог в консоль сервера
                Console.ForegroundColor = level switch
                {
                    HomeNetCore.Enums.LogLevel.Info => ConsoleColor.Green,
                    HomeNetCore.Enums.LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };
                Console.WriteLine(logEntry);
                Console.ResetColor();

                // Параллельно пишем этот же самый лог в файл на Рабочем столе!
                // Приводим твой внутренний Enum уровня к строке ("INFO", "ERROR" и т.д.)
                // 🎯 Явно кастим прилетающий уровень к енаму, который ждет файловый логгер
                fileLogger.WriteImmediately(logEntry, (HomeNetCore.Enums.LogLevel)level);

            });

            // Тестовый приветственный лог — улетит и в консоль, и в файл
            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] СИСТЕМА ДВОЙНОГО ЛОГИРОВАНИЯ ИНИЦИАЛИЗИРОВАНА ===");

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
            // 📤 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ (Корректный логгинг)
            // ============================================================================
            app.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
            {
                await _dbSemaphore.WaitAsync();
                try
                {
                    serverLogger.LogInfo($"[API] Прилетел профиль пользователя '{incomingUser.FirstName}' (ID: {incomingUser.Id})");

                    // 🎯 Получаем РЕАЛЬНЫЙ результат из репозитория
                    var resultUser = await userRepository.InsertUserAsync(incomingUser);

                    // Проверяем, совпадает ли то, что мы пытались вставить, с тем, что отдала СУБД
                    if (resultUser.Id == incomingUser.Id && resultUser.Email == incomingUser.Email)
                    {
                        // Если это реально новый объект, которого не было в базе
                        // (Так как при дубликате репозиторий возвращает старого существующего юзера)
                        if (resultUser.UpdatedAt == incomingUser.UpdatedAt)
                        {
                            serverLogger.LogInfo($"[API Успех] Пользователь '{incomingUser.FirstName}' (ID: {incomingUser.Id}) успешно физически создан в PostgreSQL.");
                        }
                        else
                        {
                            // Если репозиторий вернул существующего Ивана, сервер пишет ПРАВДУ:
                            serverLogger.LogWarning($"[API Чек] Пользователь '{incomingUser.FirstName}' уже был в системе. Повторный PUSH обработан без создания дубликата.");
                        }
                    }

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