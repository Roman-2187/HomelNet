using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Extensions;
using HomeNetServices.Diagnostics;
using HomeNetServices.Routing;
using HomeNet.Network; // 🎯 ОБЯЗАТЕЛЬНО: Подключаем namespace, где лежит ServerLogReceived

public class CoreInfrastructureModule
{
    public ILogger Logger { get; }
    public EventBusInspector EventBusInspector { get; }
    public IEventBus EventBus { get; }
    public AppFileLogger AppFileLogger { get; } // Оставляем старое имя без буквы L, раз компилятор с ним дружит
    public ILogQueueManager LogQueueManager { get; }

    public CoreInfrastructureModule()
    {
        EventBusInspector = new EventBusInspector();
        EventBus = new EventBus(EventBusInspector);
        AppFileLogger = new AppFileLogger("App_debug.txt");

        // 1. Создаем менеджер очереди UI логов
        LogQueueManager = new LogQueueManager(EventBus, 1);

        // 2. 🔥 Железобетонно собираем логгер, скармливая ему готовую логику!
        Logger = new Logger((msg, level, ns) =>
        {
            AppFileLogger.WriteImmediately(msg, level);
           // LogQueueManager.WriteLog(msg, level, ns);
        });

        LogQueueManager.SetReady();

        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
