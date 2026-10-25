using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Services;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetPresentation.ViewModels.ClientViews;

namespace HomeNet.DI.Modules
{
    public class PresentationUiModule
    {
        public BaseNavigationState Navigation { get; }
        public AdminNavigationManager AdminNavigation { get; }
        public UserNavigationManager UserNavigation { get; }

        public StatusBarViewModel StatusBarVM { get; }
        public SystemButtonsViewModel SystemButtonsVM { get; }
        public RegistrationViewModel RegistrationVM { get; }
        public AuthenticationViewModel AuthenticationVM { get; }
        public TerminalLogsViewModel TerminalLogsVM { get; }
        public InspectorViewModel InspectorVM { get; }
        public AdminViewModel AdminVM { get; }
        public DeleteUsersViewModel DeleteUsersVM { get; }
        public ChatViewModel ChatVM { get; }
        public TitleBarViewModel TitleBarVM { get; }
        public SeedUsersViewModel SeedUsersVM { get; }
        public TableUsersViewModel TableUsersVM { get; }
        public UserViewModel UserVM { get; }
        public ContactsViewModel ContactsVM { get; }
        public MainViewModel MainVM { get; }

        // 🔥 НАША НОВАЯ ВЬЮМОДЕЛЬ НАСТРОЕК ЖЕЛЕЗА
        public DeviceSettingsViewModel DeviceSettingsVM { get; }

        public PresentationUiModule(
            IEventBus eventBus,
            ILogger logger,
            ILogQueueManager logQueue,
            BusinessServicesModule services, // 🔥 ОДИН ОБЪЕКТ Вместо 6 разрозненных сервисов!
            IEventInspector inspector)
        {
            Navigation = new BaseNavigationState(eventBus);
            AdminNavigation = new AdminNavigationManager(eventBus);
            UserNavigation = new UserNavigationManager(eventBus, logger);

            StatusBarVM = new StatusBarViewModel(eventBus, Navigation);
            SystemButtonsVM = new SystemButtonsViewModel(eventBus);

            // Вытаскиваем сервисы из контейнера через точку — строго, типизировано и красиво!
            RegistrationVM = new RegistrationViewModel(services.RegistrationService, eventBus, logger, UserNavigation);
            AuthenticationVM = new AuthenticationViewModel(services.AuthenticateService, eventBus, logger, UserNavigation);
            TerminalLogsVM = new TerminalLogsViewModel(eventBus);
            InspectorVM = new InspectorViewModel(eventBus, inspector, AdminNavigation);
            AdminVM = new AdminViewModel(logQueue, eventBus, AdminNavigation);
            DeleteUsersVM = new DeleteUsersViewModel(services.DeleteService, eventBus, logger, AdminNavigation);
            ChatVM = new ChatViewModel(eventBus, services.MessageService);
            TitleBarVM = new TitleBarViewModel(eventBus, logger, Navigation);
            SeedUsersVM = new SeedUsersViewModel(eventBus, services.UserService, AdminNavigation);

            TableUsersVM = new TableUsersViewModel(eventBus, services.UserService, AdminNavigation);
            UserVM = new UserViewModel(logger, eventBus, UserNavigation);

            // 🔥 Рождаем вьюмодель настроек устройства, скармливая ей сервис из контейнера
            // Передаем строго по подсказке: сервис девайса, сервис юзеров, автобус, логгер, навигатор
            DeviceSettingsVM = new DeviceSettingsViewModel(services.DeviceSettingsService, services.UserService, eventBus, logger, UserNavigation);


            ContactsVM = new ContactsViewModel(eventBus);
            MainVM = new MainViewModel(logger, eventBus, Navigation);
        }
    }
}
