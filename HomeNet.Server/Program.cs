using HomeNet.DI.Modules;
using HomeNet.Server.Endpoints;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Infrastructure;
using HomeNetServices.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server
{
    public partial class Program
    {
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

            // Твой честный и верный пароль! 🔑
            string postgresConn = builder.Configuration.GetConnectionString("PostgresConnection")
                ?? "Server=127.0.0.1;Port=5432;Database=home_net_db;User Id=postgres;Password=05011987;";

            var postgresEngine = new SiberNetDbEngine(DbEngineType.RemoteServer, postgresConn, serverSchemas, serverLogger);

            IMessageRepository messageRepository = postgresEngine.CreateMessageRepository(null!);
            IUserRepository userRepository = postgresEngine.CreateUserRepository(null!);
            IDeviceSyncRepository deviceSyncRepository = postgresEngine.CreateDeviceSyncRepository();

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
            await base.OnConnectedAsync();
        }
    }
}
