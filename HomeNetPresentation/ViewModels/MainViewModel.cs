using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel 
    {
        private readonly ILogger _logger;

        
        
   

        public MainViewModel(ILogger logger, IEventBus eventBus ,NavigationStateManager navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           

            // 🔥 БЛИН-ТЕСТ: Заставляем бэкенд намертво бахнуть строкой в логгер при старте!
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");
        }

    }
}
