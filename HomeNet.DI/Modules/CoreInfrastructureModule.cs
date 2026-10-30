using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Extensions;
using HomeNetServices.Diagnostics;
using HomeNetServices.Routing;
using HomeNet.Network;

public class CoreInfrastructureModule
{
    public ILogger Logger { get; }
    public EventBusInspector EventBusInspector { get; }
    public IEventBus EventBus { get; }
    public ILogQueueManager LogQueueManager { get; }

    public CoreInfrastructureModule()
    {
        // 🎯 Чистое имя лог-файла клиента без всяких .txt расширений
        const string clientLogName = "client_debug";

        EventBusInspector = new EventBusInspector();
        EventBus = new EventBus(EventBusInspector);

        LogQueueManager = new LogQueueManager(EventBus, 1);

        // 🔥 НАСТРОИЛИ ПАРАЛЛЕЛЬНУЮ ОТПРАВКУ:
        // Теперь логгер одновременно пишет и в DiagnosticHub, и в очередь для UI
        Logger = new Logger((msg, level, ns) =>
        {
            // 1. Пишем в глобальный статический HTML-хаб (чтобы логи сохранялись в файл)
            DiagnosticHub.Log(msg, (HomeNetCore.Enums.LogLevel)level, clientLogName);

            // 2. Публикуем в очередь менеджера логов (чтобы они "бежали" строкой в UI)
            LogQueueManager.WriteLog(msg, (HomeNetCore.Enums.LogLevel)level, clientLogName);
        });

        LogQueueManager.SetReady();

        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
