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

        [ObservableProperty] private string _statusText = "Система готова...";
        
   

        public MainViewModel(ILogger logger, IEventBus eventBus ,NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           

            // 🔥 БЛИН-ТЕСТ: Заставляем бэкенд намертво бахнуть строкой в логгер при старте!
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");
        }

    }
}
