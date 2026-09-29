using HomeNet.DI;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;

namespace SiberNet.UI.Infrastructure
{
    public class ComponentHub
    {
        public MainViewModel MainViewModel => AppBootstrapper.GetViewModel<MainViewModel>();
        public RegistrationViewModel RegistrationViewModel => AppBootstrapper.GetViewModel<RegistrationViewModel>();
        public AuthenticationViewModel AuthenticationViewModel => AppBootstrapper.GetViewModel<AuthenticationViewModel>();
        public ChatViewModel ChatViewModel => AppBootstrapper.GetViewModel<ChatViewModel>();
        public AdminViewModel AdminMenuViewModel => AppBootstrapper.GetViewModel<AdminViewModel>();
        public UserViewModel UserProfileViewModel => AppBootstrapper.GetViewModel<UserViewModel>();
        public TitleBarViewModel TitleBarViewModel => AppBootstrapper.GetViewModel<TitleBarViewModel>();
       
        public TerminalLogsViewModel TerminalViewModel => AppBootstrapper.GetViewModel<TerminalLogsViewModel>();
        public TableUsersViewModel TableUsersViewModel => AppBootstrapper.GetViewModel<TableUsersViewModel>();
        public InspectorViewModel InspectorViewModel => AppBootstrapper.GetViewModel<InspectorViewModel>();
        public SeedUsersViewModel SeedUsersViewModel => AppBootstrapper.GetViewModel<SeedUsersViewModel>();
        public DeleteUsersViewModel DeleteUsersViewModel => AppBootstrapper.GetViewModel<DeleteUsersViewModel>();
        public StatusBarViewModel StatusBarViewModel => AppBootstrapper.GetViewModel<StatusBarViewModel>();
    }


}
