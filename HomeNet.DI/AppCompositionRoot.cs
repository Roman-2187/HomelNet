using HomeNet.DI.Modules;
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetOrm.Interfaces.Builders;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;

namespace HomeNet.DI
{
    public class AppCompositionRoot
    {
        private static AppCompositionRoot? _instance;
        public static AppCompositionRoot Instance => _instance
            ?? throw new InvalidOperationException("Композиция не собрана. Вызовите AppCompositionRoot.Initialize(...)");

        // 🔥 ХРАНИМ ЧЕТЫРЕ МОДУЛЯ-КОНТЕЙНЕРА ВМЕСТО 50 РАЗРОЗНЕННЫХ ПОЛЕЙ
        private readonly CoreInfrastructureModule _infrastructure;
        private readonly DataAccessModule _dataAccess;
        private readonly BusinessServicesModule _services;
        private readonly PresentationUiModule _ui;

        // Свойства-мосты наружу для совместимости бэкенда (если нужны)
        public ILogger Logger => _infrastructure.Logger;
        public IEventBus EventBus => _infrastructure.EventBus;

        private AppCompositionRoot(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 1. Модуль Системных Кишок
            _infrastructure = new CoreInfrastructureModule();

            // 2. 🎯 СОБИРАЕМ СХЕМЫ СНАРУЖИ ЧЕРЕЗ НАШ НОВЫЙ DI-МОДУЛЬ
            var schemaModule = new MessengerSchemaModule();
            var registeredSchemas = schemaModule.GetRegisteredSchemas();

            // 3. Модуль Базы Данных и ОРМ (Скармливаем зависимости, адрес сервера и готовые схемы!) 🦾
            _dataAccess = new DataAccessModule(
                sqliteConn,
                postgresConn, // В контексте клиента это базовый URL сервера (apiBaseUrl)
                registeredSchemas, // 🔥 Вот они улетели в ОРМ-движок!
                _infrastructure.Logger,_infrastructure.LogQueueManager
                ,
                _infrastructure.EventBus);

            // 4. Модуль Сервисов Бизнес-Логики (Скармливаем Репозитории из Модуля 3)
            _services = new BusinessServicesModule(
                _dataAccess.UserRepository, _dataAccess.MessageRepository,
                _infrastructure.Logger, _infrastructure.EventBus);

            // 5. Модуль Презентации / UI (Скармливаем Логику и Систему логов)
            _ui = new PresentationUiModule(
                _infrastructure.EventBus, _infrastructure.Logger,
                _infrastructure.LogQueueManager, _services.UserService,
                _services.RegistrationService, _services.AuthenticateService,
                _services.DeleteService, _services.MessageService,
                _infrastructure.EventBusInspector
            );
        }

        public static void Initialize(BackendMode mode, string postgresConn, string sqliteConn)
        {
            _instance = new AppCompositionRoot(mode, postgresConn, sqliteConn);

            // 1. Подписываемся на автобус готовности инфраструктуры
            _instance._infrastructure.EventBus.Subscribe<IDbContextContainer.InfrastructureReady>(async ev =>
            {
                _instance._infrastructure.Logger.LogInfo("[App Старт] Базы и сеть прогреты. Запускаем репликацию данных...");

                // 🔥 ПОДКЛЮЧАЕМ ТРУБУ SIGNALR: Врубаем коннект гостем, чтобы забрать буфер серверных логов!
                try
                {
                    await _instance._dataAccess.NotificationListener.ConnectAsync();
                }
                catch (Exception ex)
                {
                    _instance._infrastructure.Logger.LogError($"[App Старт] Не удалось подключить логи бэкенда: {ex.Message}");
                }

                // Добавим микроскопическую задержку в 300 мс, чтобы дать веб-серверу окончательно "продышаться"
                await Task.Delay(300);

                // 🔥 ТЕПЕРЬ ЗАПУСК СТРОГО ВО ВРЕМЯ!
                await _instance._dataAccess.OrmEngine.StartAsync();
            });

            // 2. Только после подписки пинаем параллельный прогрев баз и пинг сервера
            Task.Run(async () =>
            {
                await _instance._dataAccess.DbContextContainer.InitializeAllDatabasesAsync();
            });
        }


        // Метод-заглушка для UI окон вытаскивает вьюмодели прямо из UI модуля
        public TGet GetViewModel<TGet>() where TGet : class
        {
            if (typeof(TGet) == typeof(TableUsersViewModel)) return (_ui.TableUsersVM as TGet)!;
            if (typeof(TGet) == typeof(UserViewModel)) return (_ui.UserVM as TGet)!;
            if (typeof(TGet) == typeof(MainViewModel)) return (_ui.MainVM as TGet)!;
            if (typeof(TGet) == typeof(ChatViewModel)) return (_ui.ChatVM as TGet)!;
            if (typeof(TGet) == typeof(StatusBarViewModel)) return (_ui.StatusBarVM as TGet)!;
            if (typeof(TGet) == typeof(SystemButtonsViewModel)) return (_ui.SystemButtonsVM as TGet)!;
            if (typeof(TGet) == typeof(RegistrationViewModel)) return (_ui.RegistrationVM as TGet)!;
            if (typeof(TGet) == typeof(AuthenticationViewModel)) return (_ui.AuthenticationVM as TGet)!;
            if (typeof(TGet) == typeof(TerminalLogsViewModel)) return (_ui.TerminalLogsVM as TGet)!;
            if (typeof(TGet) == typeof(InspectorViewModel)) return (_ui.InspectorVM as TGet)!;
            if (typeof(TGet) == typeof(AdminViewModel)) return (_ui.AdminVM as TGet)!;
            if (typeof(TGet) == typeof(DeleteUsersViewModel)) return (_ui.DeleteUsersVM as TGet)!;
            if (typeof(TGet) == typeof(TitleBarViewModel)) return (_ui.TitleBarVM as TGet)!;
            if (typeof(TGet) == typeof(SeedUsersViewModel)) return (_ui.SeedUsersVM as TGet)!;
            if (typeof(TGet) == typeof(ContactsViewModel)) return (_ui.ContactsVM as TGet)!;

            throw new ArgumentException($"Тип {typeof(TGet).Name} не найден в UI模块.");
        }
    }
}
