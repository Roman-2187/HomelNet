using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Infrastructure;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Threading; // 🎯 Подключаем для семафора

public partial class Program
{
    // 🎯 Единый турникет для защиты нашего persistent-подключения Postgres
    private static readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Поднимаем боевой логгер (работает строго с консолью)
        HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger = Logger.CreateConsoleLogger();

        // 2. СОБИРАЕМ СХЕМЫ СНАРУЖИ ЧЕРЕЗ НАШ DI-МОДУЛЬ
        var schemaModule = new HomeNet.DI.Modules.MessengerSchemaModule();
        var myAppSchemas = schemaModule.GetRegisteredSchemas();

        // 3. Строка подключения к PostgreSQL
        string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
            ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

        // 4. ЧИСТАЯ СБОРКА ЧЕРЕЗ ДВИЖОК
        var postgresEngine = new PostgresDbEngine(postgresConn, myAppSchemas, serverLogger);

        IMessageRepository messageRepository = postgresEngine.CreateMessageRepository();
        IUserRepository userRepository = postgresEngine.CreateUserRepository();

        builder.Services.AddSignalR();
        var app = builder.Build();

        app.MapHub<ServerChatHub>("/chatHub");

        // ============================================================================
        // 🎯 ЭНДПОИНТ: ПРИЕМ СООБЩЕНИЙ ЧЕРЕЗ РЕПОЗИТОРИЙ
        // ============================================================================
        app.MapPost("/api/messages", async (MessageEntity incomingMessage, IHubContext<ServerChatHub> hubContext) =>
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId} для {incomingMessage.ReceiverId}");

                await messageRepository.SaveMessageAsync(incomingMessage);

                // 🎯 ДОБАВИЛИ ЛОГ УСПЕХА: Теперь видно, что СМС реально легло в Postgres
                serverLogger.LogInfo($"[API Успех] Сообщение {incomingMessage.Id} успешно записано в PostgreSQL.");

                // Мгновенно пушаем получателю по сети
                await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", incomingMessage);

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
        // 🎯 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ
        // ============================================================================
        app.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                serverLogger.LogInfo($"[API] Прилетел профиль пользователя '{incomingUser.FirstName}' (ID: {incomingUser.Id})");

                await userRepository.InsertUserAsync(incomingUser);

                // 🎯 ДОБАВИЛИ ЛОГ УСПЕХА: Теперь в консоли четко будет видно, кто зашел
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
        // 🎯 ЭНДПОИНТ: УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ
        // ============================================================================
        app.MapDelete("/api/users/{id:guid}", async (Guid id, IHubContext<ServerChatHub> hubContext) =>
        {
            await _dbSemaphore.WaitAsync(); // 🎯 Встаем в очередь
            try
            {
                serverLogger.LogInfo($"[API] Запрос на удаление пользователя (ID: {id})");

                await userRepository.DeleteByIdAsync(id);

                // Оповещаем клиентов об удалении
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
                _dbSemaphore.Release(); // 🎯 Освобождаем коннект
            }
        });

        // ============================================================================
        // 📦 ИНИЦИАЛИЗАЦИЯ И ЗАПУСК
        // ============================================================================
        serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");

        await postgresEngine.InitializeChannelAsync(); // Запуск ОРМ-проверки

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
