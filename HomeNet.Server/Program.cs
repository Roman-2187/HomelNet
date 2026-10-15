using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Repositories;    // Для MessageRepository и UserRepository
using Microsoft.AspNetCore.SignalR;
using ILogger = HomeNetCore.Interfaces.Diagnostics.ILogger;

var builder = WebApplication.CreateBuilder(args);

// 1. Поднимаем боевой логгер
ILogger serverLogger = new HomeNetServices.Diagnostics.Logger((logEntry, level, detectedNamespace) =>
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

// 2. Строка подключения и серверный автобус (чтобы репозитории могли стрелять)
string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
    ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

IEventBus serverEventBus = new MockServerEventBus(); // Для сервера события шины не критичны, даем заглушку

// 3. 💥 ЧИСТАЯ СБОРКА ЧЕРЕЗ NEW: Поднимаем движок и репозитории
var postgresEngine = new PostgresDbEngine(postgresConn, serverLogger);
IMessageRepository messageRepository = new MessageRepository(postgresEngine, serverEventBus);
IUserRepository userRepository = new UserRepository(postgresEngine, serverEventBus);

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

        // 🦾 РАБОТАЕМ СТРОГО ЧЕРЕЗ РЕПОЗИТОРИЙ, КАК В СЕРВИСАХ!
        // Никакого SQL, никаких генераторов наружу — чистый объект ложится в Postgres!
        await messageRepository.SaveMessageAsync(incomingMessage);

        // Мгновенно пушим получателю по сети
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

// Запускаем инициализацию Postgres на сервере
serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");
await postgresEngine.InitializeChannelAsync(); // Это заставит ОРМ проверить таблицы по сети!

app.Run();

// Заглушка шины для серверных репозиториев
// Заглушка шины для серверных репозиториев с явной реализацией интерфейса
public class MockServerEventBus : IEventBus
{
    public void Subscribe<TMessage>(Action<TMessage> action)
    {
        // Пусто — на сервере подписки не нужны
    }

    public void Unsubscribe<TMessage>(Action<TMessage> action)
    {
        // Пусто
    }

    public void Publish<TMessage>(object sender, TMessage ev)
    {
        // Пусто
    }
}


public class ServerChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId)) await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}

