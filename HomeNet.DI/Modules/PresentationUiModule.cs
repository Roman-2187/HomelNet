using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Services;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;

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

        public PresentationUiModule(
            IEventBus eventBus,
            ILogger logger,
            ILogQueueManager logQueue,
            IUserService userService,
            IRegistrationService regService,
            IAuthenticateService authService,
            IDeleteService delService,
            IMessageService msgService,IEventInspector inspector)
        {
            Navigation = new BaseNavigationState(eventBus);
            AdminNavigation = new AdminNavigationManager(eventBus);
            UserNavigation = new UserNavigationManager(eventBus, logger);

            StatusBarVM = new StatusBarViewModel(eventBus,Navigation);
            SystemButtonsVM = new SystemButtonsViewModel(eventBus);
            RegistrationVM = new RegistrationViewModel(regService,eventBus,logger,UserNavigation);
            AuthenticationVM = new AuthenticationViewModel(authService,eventBus,logger,UserNavigation);
            TerminalLogsVM = new TerminalLogsViewModel(eventBus);
            InspectorVM = new InspectorViewModel(eventBus,inspector,AdminNavigation);
            AdminVM = new AdminViewModel(logQueue,eventBus,AdminNavigation);
            DeleteUsersVM = new DeleteUsersViewModel(delService,eventBus,logger,AdminNavigation);
            ChatVM = new ChatViewModel(eventBus,msgService);
            TitleBarVM = new TitleBarViewModel(eventBus,logger,Navigation);
            SeedUsersVM = new SeedUsersViewModel(eventBus,userService,AdminNavigation);

            TableUsersVM = new TableUsersViewModel(eventBus, userService, AdminNavigation);
            UserVM = new UserViewModel(logger, eventBus, UserNavigation);

            ContactsVM = new ContactsViewModel(eventBus);
            MainVM = new MainViewModel(logger, eventBus,Navigation);
        }
    }
}
