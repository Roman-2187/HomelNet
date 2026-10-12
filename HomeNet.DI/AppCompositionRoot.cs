using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNet.DI.Modules; 

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

            // 2. Модуль Базы Данных и ОРМ (Скармливаем зависимости из Модуля 1)
            _dataAccess = new DataAccessModule(
                sqliteConn, postgresConn,
                _infrastructure.Logger, _infrastructure.EventBus);

            // 3. Модуль Сервисов Бизнес-Логики (Скармливаем Репозитории из Модуля 2)
            _services = new BusinessServicesModule(
                _dataAccess.UserRepository, _dataAccess.MessageRepository,
                _dataAccess.FriendRepository, _infrastructure.Logger,
                _infrastructure.EventBus);

            // 4. Модуль Презентации / UI (Скармливаем Логику и Систему логов)
            _ui = new PresentationUiModule(
                _infrastructure.EventBus, _infrastructure.Logger,
                _infrastructure.LogQueueManager,_services.UserService,
                _services.RegistrationService, _services.AuthenticateService,
                _services.DeleteService, _services.MessageService,
                _infrastructure.EventBusInspector
            );
        }

        public static AppCompositionRoot Initialize(BackendMode mode, string postgresConn, string sqliteConn)
        {
            _instance = new AppCompositionRoot(mode, postgresConn, sqliteConn);

            Task.Run(async () =>
            {
                // Инициализируем каналы движков и запускаем оффлайн-координатор
                await _instance._dataAccess.DbContextContainer.InitializeAllDatabasesAsync();
                await _instance._dataAccess.SyncCoordinator.StartAsync();
            });

            return _instance;
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

            throw new ArgumentException($"Тип {typeof(TGet).Name} не найден в UI модуле.");
        }
    }
}
