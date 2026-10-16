using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Infrastructure;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using ILogger = HomeNetCore.Interfaces.Diagnostics.ILogger;

var builder = WebApplication.CreateBuilder(args);

// 1. Поднимаем боевой логгер
ILogger serverLogger = new Logger((logEntry, level, detectedNamespace) =>
{
    Console.ForegroundColor = level switch
    {
        HomeNetCore.Enums.LogLevel.Info => ConsoleColor.Green,
        HomeNetCore.Enums.LogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.White
    };
    Console.WriteLine(logEntry);
    Console.ResetColor();
});

// 2. Строка подключения к PostgreSQL
string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
    ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

// 3. 🦾 ЧИСТАЯ СБОРКА ЧЕРЕЗ ФАБРИКУ ДВИЖКА (без всяких фейковых автобусов!)
var postgresEngine = new PostgresDbEngine(postgresConn, serverLogger);

// Движок сам знает, как собрать репозитории на базе своего DbConnection и internal-генераторов!
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
    try
    {
        serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId}");

        // Чистый объект ложится в Postgres через фабричный репозиторий движка!
        await messageRepository.SaveMessageAsync(incomingMessage);

        // Мгновенно пушим получателю по сети через SignalR
        await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString())
            .SendAsync("ReceiveMessage", incomingMessage);

        return Results.Ok(new { Success = true });
    }
    catch (Exception ex)
    {
        serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
        return Results.Problem("Ошибка сервера");
    }
});

// ============================================================================
// 🎯 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ/ОБНОВЛЕНИИ
// ============================================================================
app.MapPost("/api/users", async (UserEntity incomingUser) =>
{
    try
    {
        serverLogger.LogInfo($"[API] Прилетел профиль пользователя '{incomingUser.FirstName}' (ID: {incomingUser.Id})");

        // Клади честно в Postgres через серверный репозиторий
        await userRepository.InsertUserAsync(incomingUser);

        return Results.Ok(new { Success = true });
    }
    catch (Exception ex)
    {
        serverLogger.LogError($"[API Ошибка] Не удалось сохранить пользователя через репозиторий: {ex.Message}");
        return Results.Problem("Ошибка сервера");
    }
});


app.MapGet("/api/ping", () => Results.Ok("pong"));


// Запускаем инициализацию Postgres на сервере
serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");
await postgresEngine.InitializeChannelAsync(); // Это заставит ОРМ проверить таблицы по сети!

app.Run();

public class ServerChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId)) await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}
