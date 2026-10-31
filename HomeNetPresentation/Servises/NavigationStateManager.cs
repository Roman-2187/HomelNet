using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Models.Entity;

namespace HomeNetPresentation.Services
{
    public partial class BaseNavigationState : ObservableObject, IDisposable
    {
        protected readonly IEventBus EventBus;

        // Единое сквозное свойство для привязки UI и подгрузки ID
        [ObservableProperty] private MainTab _currentMainZone = MainTab.StartZone;
        [ObservableProperty] private UserEntity? _currentUser;

        public BaseNavigationState(IEventBus eventBus)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public virtual void Dispose() { }
    }
}
