using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using Microsoft.Extensions.DependencyInjection;

namespace HomeNet.DI
{
    public class ComponentHub
    {
        // Пустой конструктор, чтобы XAML мог его создать
        public ComponentHub() { }

        // Каждое свойство безопасно лезет в провайдер ядра
        public MainViewModel MainViewModel => GetViewModelSafe<MainViewModel>();
        public TitleBarViewModel TitleBarViewModel => GetViewModelSafe<TitleBarViewModel>();
        public SystemButtonsViewModel SystemButtonsViewModel => GetViewModelSafe<SystemButtonsViewModel>();
        public StatusBarViewModel StatusBarViewModel => GetViewModelSafe<StatusBarViewModel>();

        public AuthenticationViewModel AuthenticationViewModel => GetViewModelSafe<AuthenticationViewModel>();
        public RegistrationViewModel RegistrationViewModel => GetViewModelSafe<RegistrationViewModel>();
        public AdminViewModel AdminViewModel => GetViewModelSafe<AdminViewModel>();
        public UserViewModel UserProfileViewModel => GetViewModelSafe<UserViewModel>();

        public TableUsersViewModel TableUsersViewModel => GetViewModelSafe<TableUsersViewModel>();
        public DeleteUsersViewModel DeleteUsersViewModel => GetViewModelSafe<DeleteUsersViewModel>();
        public TerminalLogsViewModel TerminalLogsViewModel => GetViewModelSafe<TerminalLogsViewModel>();
        public InspectorViewModel InspectorViewModel => GetViewModelSafe<InspectorViewModel>();

      public  SeedUsersViewModel SeedUsersViewModel => GetViewModelSafe<SeedUsersViewModel>();

        private T GetViewModelSafe<T>() where T : class
        {
            try
            {
                // В реальном приложении забираем настоящий живой инстанс
                return AppBootstrapper.GetViewModel<T>();
            }
            catch
            {
                // 🔥 СПАСЕНИЕ ДИЗАЙНЕРА: если приложение не запущено,
                // возвращаем null, но без вылета исключения, чтобы Авалония не падала
                return null!;
            }
        }
    }
}
