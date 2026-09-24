using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetPresentation.ViewModels
{
    public partial class AccountMenuViewModel : ObservableObject
    {
        private readonly IEventBus _eventBus;

        // Динамический заголовок плашки (будет меняться: "ДРУЗЬЯ" / "АДМИНКА")
        [ObservableProperty] private string _title = "МЕНЮ";

        // Управление состоянием открытия шторки (Popup) из кода, если понадобится
        [ObservableProperty] private bool _isMenuOpen;

        public AccountMenuViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        [RelayCommand]
        private void OpenProfile()
        {
            IsMenuOpen = false;
            // Здесь будет логика открытия профиля (например, через навигатор или шину)
            // _eventBus.Publish(this, new IMainViewModel.OpenProfileRequest());
        }

      


        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false; // Схлопываем шторку
            _eventBus.Publish(this, new ITitleBarViewModel.MacroNavigation(MainTab.None));
        }


    }
}
