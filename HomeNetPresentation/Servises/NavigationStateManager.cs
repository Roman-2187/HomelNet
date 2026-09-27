using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    public partial class NavigationStateManager : ObservableObject, IDisposable
    {
        protected readonly IEventBus EventBus;

        // Единое сквозное свойство для привязки UI и подгрузки ID
        [ObservableProperty] private MainTab _currentMainZone = MainTab.StartZone;
        [ObservableProperty] private UserEntity? _currentUser;

        public NavigationStateManager(IEventBus eventBus)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public virtual void Dispose() { }
    }
}
