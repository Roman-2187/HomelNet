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
    public AppFileLogger AppFileLogger { get; }
    public ILogQueueManager LogQueueManager { get; }

    public CoreInfrastructureModule()
    {
        EventBusInspector = new EventBusInspector();
        EventBus = new EventBus(EventBusInspector);

        // Передаем только имя файла. Логгер сам создаст его на Рабочем столе в папке debug
        AppFileLogger = new AppFileLogger("client_debug.txt");

        LogQueueManager = new LogQueueManager(EventBus, 1);

        Logger = new Logger((msg, level, ns) =>
        {
            AppFileLogger.WriteImmediately(msg, level);
        });

        LogQueueManager.SetReady();

        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
