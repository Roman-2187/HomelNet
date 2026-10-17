using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Infrastructure;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // 🎯 Обязательно для List<string>

public partial class Program
{
    // 🔥 НАШ ЛОКАЛЬНЫЙ БУФЕР ДЛЯ СБОРКА СТАРТОВЫХ ЛОГОВ
    public static readonly List<string> StartupLogs = new();

    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Поднимаем боевой логгер
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
            try
            {
                serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId}");

                await messageRepository.SaveMessageAsync(incomingMessage);

                // Мгновенно пушаем получателю по сети
                await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", incomingMessage);

                // 🔥 ЖИВОЙ СТРИМ: Публикуем текущий лог в терминал клиента прямо из эндпоинта!
                await hubContext.Clients.All.SendAsync("ReceiveServerLog", $"[SERVER] Успешно обработано СМС от {incomingMessage.SenderId}");

                return Results.Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
                return Results.Problem("Ошибка сервера");
            }
        });

        // ============================================================================
        // 🎯 ЭНДПОИНТ: ПРИЕМ ПОЛЬЗОВАТЕЛЕЙ ПРИ РЕГИСТРАЦИИ
        // ============================================================================
        app.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
        {
            try
            {
                serverLogger.LogInfo($"[API] Прилетел профиль пользователя '{incomingUser.FirstName}' (ID: {incomingUser.Id})");

                await userRepository.InsertUserAsync(incomingUser);

                // 🔥 ЖИВОЙ СТРИМ: Отправляем лог регистрации в клиент
                await hubContext.Clients.All.SendAsync("ReceiveServerLog", $"[SERVER] Профиль '{incomingUser.FirstName}' успешно заведен в Postgres.");

                return Results.Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                serverLogger.LogError($"[API Ошибка] Не удалось сохранить пользователя через репозиторий: {ex.Message}");
                return Results.Problem("Ошибка сервера");
            }
        });

        app.MapGet("/api/ping", () => Results.Ok("pong"));

        // ============================================================================
        // 📦 ЗАПИСЬ ИНИЦИАЛИЗАЦИИ В ЛОКАЛЬНЫЙ БУФЕР СТАРТА
        // ============================================================================
        serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");

        StartupLogs.Add("[SERVER START] === ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");
        StartupLogs.Add("[SERVER START] Сверяю структуру таблиц 'users', 'messages', 'friends' по внешним схемам...");

        await postgresEngine.InitializeChannelAsync(); // Запуск ОРМ-проверки

        StartupLogs.Add("[SERVER START] ✅ База данных PostgreSQL успешно прогрета. Все таблицы соответствуют схемам.");
        StartupLogs.Add("[SERVER START] Веб-сервер запущен и слушает порт.");

        app.Run();
    }
}
public class ServerChatHub : Hub
{
    // 🔥 ФИКС: Добавляем async перед Task в заголовке метода!
    public override async Task OnConnectedAsync()
    {
        string? userId = Context.GetHttpContext()?.Request.Query["userId"];

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        // 🔥 МАГИЯ ТАЙМИНГА: Теперь никакого Task.Run! 
        // Спокойно ждем 300 мс прямо тут, давая Авалонии повесить подписку на шину
        await Task.Delay(300);

        // И честно выгружаем накопленную пачку логов ОРМ-инициализации!
        lock (Program.StartupLogs)
        {
            foreach (var startupLog in Program.StartupLogs)
            {
                // Отправляем точечно именно этому подключившемуся клиенту
                _ = Clients.Caller.SendAsync("ReceiveServerLog", startupLog);
            }
        }

        // 🔥 ФИКС: Обязательно добавляем await перед вызовом базового метода!
        await base.OnConnectedAsync();
    }
}



