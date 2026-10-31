using HomeNetCore.Interfaces.Diagnostics;
using HomeNetServices.Diagnostics;
using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

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

        // 2. Инициализируем менеджер очереди логов для UI
        LogQueueManager = new LogQueueManager(EventBus, 1);

        // 3. 🔥 НАСТРАИВАЕМ НАШ ОБНОВЛЕННЫЙ SiberNetLogger
        // В Action передаем только то, что должно уходить дальше (в очередь UI)
        Logger = new SiberNetLogger((logEntry, level, ns) =>
        {
            
            LogQueueManager.WriteLog(logEntry, level, ns);
        }, clientLogName);

        // 4. Запускаем воркер очереди логов
        LogQueueManager.SetReady();

        // 5. Стреляем первым логом через обновленный интерфейс
        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
