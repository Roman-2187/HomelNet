using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ICastomLogger _logger;
        private readonly IEventBus _eventBus;

        public MainViewModel(ICastomLogger logger, IEventBus eventBus, BaseNavigationState navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<WindowOpenedEvent>(e =>
            {
                _logger.LogInfo($"[PERF] Окно открыто через {e.ElapsedMs} мс");
            });
        }
    }


    public class WindowOpenedEvent
    {
        public long ElapsedMs { get; set; }
    }
}
