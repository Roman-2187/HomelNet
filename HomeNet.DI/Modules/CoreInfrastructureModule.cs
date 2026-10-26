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

        // 🎯 Переводим клиентский логгер на глобальный статический HTML-хаб
        Logger = new Logger((msg, level, ns) =>
        {
            DiagnosticHub.Log(msg, (HomeNetCore.Enums.LogLevel)level, clientLogName);
        });

        LogQueueManager.SetReady();

        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
