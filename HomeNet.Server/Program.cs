using HomeNet.DI.Modules;
using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Server.Endpoints;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Remote;
using Microsoft.AspNetCore.SignalR;


namespace HomeNet.Server
{
    public partial class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            const string serverLogName = "server_debug";

            // 1. 🪐 СОБИРАЕМ МНОГОСТВОЛЬНЫЙ ЛОГГЕР
            ICastomLogger serverLogger = new AggregateLogger(
                new ConsoleLogger(),
                new HtmlLogger(serverLogName)
            );

            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] СИСТЕМА ДВОЙНОГО ЛОГИРОВАНИЯ ИНИЦИАЛИЗИРОВАНА ===");

            var schemaModule = new RemoteMessengerSchemaModule();
            var serverSchemas = schemaModule.GetSchemas();

            string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
                ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

            // 2. Создаем движок для PostgreSQL
            var postgresEngine = new SiberNetDbEngine(DbEngineType.PostgreSQLServer, postgresConn, serverSchemas, serverLogger);

            // 🔥 3. Инициализируем фабрику репозиториев БЕЗ шины событий (null)
            // Серверу не нужны факты, он просто пишет в базу.
            postgresEngine.InitializeRepositories(null);

            // 🔥 4. Достаем готовые репозитории из фабрики
                IMessageRepo messageRepository = postgresEngine.Repositories.GetRepository<IMessageRepo>();
                IUserRepo userRepository = postgresEngine.Repositories.GetRepository<IUserRepo>();
                DeviceSyncRepository deviceSyncRepository = postgresEngine.Repositories.GetRepository<DeviceSyncRepository>();

                // 🔥 РЕГИСТРИРУЕМ В DI-КОНТЕЙНЕРЕ ASP.NET CORE:
                builder.Services.AddSingleton(userRepository);
               

                builder.Services.AddSignalR();
                var app = builder.Build();

            // --- МАППИНГ РАЗДЕЛЕННЫХ СЛОЕВ ЭНДПОИНТОВ ---
            app.MapMessageEndpoints(messageRepository, serverLogger);
            app.MapUserEndpoints(userRepository, serverLogger);
            app.MapSyncEndpoints(deviceSyncRepository, serverLogger);

            // Сетевой автобус SignalR
            app.MapHub<ServerChatHub>("/chatHub");

            // Инициализация базы данных строго перед стартом самого веб-сервера!
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

            // 🔥 РУКОПОЖАТИЕ SIBERNET:
            await Clients.Caller.SendAsync("ReceiveServerReady");

            await base.OnConnectedAsync();
        }
    }
}