using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetPresentation.Services; 

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public MainViewModel(ILogger logger, IEventBus eventBus, BaseNavigationState navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
    }
}
