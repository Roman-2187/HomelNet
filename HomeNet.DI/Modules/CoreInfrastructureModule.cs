using HomeNetCore.Interfaces.Diagnostics;
using HomeNetServices.Diagnostics;
using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Diagnostics.EventInspector;

public class CoreInfrastructureModule
{
    public ICastomLogger Logger { get; }
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


        // 🔥 Собираем трехствольный логгер для клиента: Консоль + HTML + Экран приложения!
        Logger = new AggregateLogger(
        new HtmlLogger(clientLogName),
        new QueueLogger(entry => LogQueueManager.WriteLog(entry)) 
       );

        // 4. Запускаем воркер очереди логов
        LogQueueManager.SetReady();

        // 5. Стреляем первым логом через обновленный интерфейс
        Logger.LogInfo("[Инфраструктура] Логгер успешно поднят и настроен.");
    }
}
