using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel : FormViewModelBase, IMainViewModel
    {
        private readonly ILogger _logger;

        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.Authentication;
        [ObservableProperty] private string _statusText = "Система готова...";
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;

        // 🔥 НАШ ГЛАВНЫЙ СИЛОВОЙ КАБЕЛЬ: Передаёт все энумы в XAML окна!
       

        public MainViewModel(ILogger logger, IEventBus eventBus ,NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeBusSubscriptions();

            // 🔥 БЛИН-ТЕСТ: Заставляем бэкенд намертво бахнуть строкой в логгер при старте!
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");
        }

        private void InitializeBusSubscriptions()
        {
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg => OnAuthSuccess(msg.User, msg.IsFromAdminPanel));
            EventBus.Subscribe<IUsersTableViewModel.Added>(msg => OnAuthSuccess(msg.User, false));

           
        }

        private void OnAuthSuccess(UserEntity user, bool isFromAdminPanel)
        {
            if (user == null) return;

            if (isFromAdminPanel)
            {
                CurrentMainTab = MainTab.AdminZone;
            }
            else
            {
                CurrentMainTab = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger;
            }
        }
    }
}
