using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using System;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Вьюмодель выпадающего меню аккаунта SiberNet.
    /// Полностью изолирована: отправляет намерения пользователя напрямую в шину событий.
    /// </summary>
    public partial class AccountMenuViewModel : ObservableObject
    {
        private readonly IEventBus _eventBus;

        // Динамический заголовок плашки (будет меняться: "ДРУЗЬЯ" / "АДМИНКА")
        [ObservableProperty] private string _title = "МЕНЮ";

        // Управление состоянием открытия шторки (Popup) из кода
        [ObservableProperty] private bool _isMenuOpen;

        public AccountMenuViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        [RelayCommand]
        private void OpenProfile()
        {
            IsMenuOpen = false; // Схлопываем шторку

            // 🔥 ЧИСТОТА: Пуляем в автобус официальный рекорд запроса открытия профиля!
            // Навигатор пользователя поймает его и переключит ClientSubTab в Profile.
            _eventBus.Publish(this, new IUserVm.OpenProfile());
        }

        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false; // Схлопываем шторку

            // 🔥 ДИСЦИПЛИНА: Отправляем официальный рекорд полного выхода клиента.
            // Навигатор сам поймает его, очистит сессию, занулит текущего юзера и сбросит макро-зону в StartZone.
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }
    }
}
