using HomeNet.DI;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;


namespace SiberNet.UI.Infrastructure
{
    public class ComponentHub
    {
        // 👤 Главная вьюмодель окна (для автомата состояний и роутинга)
        public MainViewModel MainViewModel => AppBootstrapper.GetViewModel<MainViewModel>();

        // 📝 Вьюмодель формы регистрации пользователей
        public RegistrationViewModel RegistrationViewModel => AppBootstrapper.GetViewModel<RegistrationViewModel>();

        // 🔑 Вьюмодель формы входа (Authentication)
        public AuthenticationViewModel AuthenticationViewModel => AppBootstrapper.GetViewModel<AuthenticationViewModel>();

        // 💬 Вьюмодель мессенджера / чата
        public ChatViewModel ChatViewModel => AppBootstrapper.GetViewModel<ChatViewModel>();

        public AdminMenuViewModel AdminMenuViewModel => AppBootstrapper.GetViewModel<AdminMenuViewModel>();
  
        public UserDashboardViewModel UserDashboardViewModel => AppBootstrapper.GetViewModel<UserDashboardViewModel>();

        public TitleBarViewModel TitleBarViewModel => AppBootstrapper.GetViewModel<TitleBarViewModel>();

        public AccountMenuViewModel AccountMenuViewModel => AppBootstrapper.GetViewModel<AccountMenuViewModel>();

        public TerminalLogsViewModel TerminalViewModel => AppBootstrapper.GetViewModel<TerminalLogsViewModel>();

       
        public TableUsersViewModel TableUsersViewModel => AppBootstrapper.GetViewModel<TableUsersViewModel>();

        public InspectorViewModel InspectorViewModel => AppBootstrapper.GetViewModel<InspectorViewModel>();

        public SeedUsersViewModel SeedUsersViewModel => AppBootstrapper.GetViewModel<SeedUsersViewModel>();

        // ⚔️ Модуль хирургического удаления пользователей
        public DeleteUsersViewModel DeleteUsersViewModel => AppBootstrapper.GetViewModel<DeleteUsersViewModel>();


    }
}
