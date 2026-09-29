using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetPresentation.Services
{
    /// <summary>
    /// Автономный менеджер навигации зоны пользователя мессенджера SiberNet.
    /// Напрямую рулит гостевыми экранами и мессенджером, отправляя полный дуэт стейтов.
    /// </summary>
    public partial class UserNavigationManager : NavigationStateManager
    {
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;

        public UserNavigationManager(IEventBus eventBus) : base(eventBus)
        {
            EventBus.Subscribe<IUserVm.OpenAuth>(OnOpenAuthRequested);
            EventBus.Subscribe<IUserVm.OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Subscribe<IUserVm.BackToStart>(OnBackToStartRequested);

            // 🔥 ПОДПИСЫВАЕМСЯ НА ОБА БРАУЗЕРНЫХ ВЕБ-ИВЕНТА С КНОПОК
            EventBus.Subscribe<IUserVm.UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUpRequested); // Ловим успех регистрации!

            EventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }

        #region 👤 МАКРО-УПРАВЛЕНИЕ ЗОНОЙ ПОЛЬЗОВАТЕЛЯ И ГОСТЯ

        private void OnOpenAuthRequested(IUserVm.OpenAuth msg)
        {
            CurrentMainZone = MainTab.StartZone; // Мы всё ещё в стартовой локации
            CurrentClientTab = ClientSubTab.Authentication; // Но включили форму входа

            // 🔥 ПИНГ-ПОНГ: Выстреливаем дуэтом стейтов обратно в тайтлбар!
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        private void OnOpenRegistrationRequested(IUserVm.OpenRegistration msg)
        {
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.Registration;

            // 🔥 ПИНГ-ПОНГ: Выстреливаем дуэтом стейтов!
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        private void OnBackToStartRequested(IUserVm.BackToStart msg)
        {
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None; // Потушили за собой форму гостя

            // 🔥 ПИНГ-ПОНГ: Полный сброс в стартовые нули! Кнопки вернутся.
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        private void OnUserAuthenticatedRequested(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone; // Переключили рубильник вертикали на Клиента!
            CurrentClientTab = ClientSubTab.Messenger; // Переключили внутренний стейт на мессенджер

            // 🔥 ПИНГ-ПОНГ: Отправляем стейты успешного входа! В XAML откроется мессенджер.
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        // 🔥 ДОБАВЛЕННЫЙ МЕТОД: Обработка успешного SignUp (Регистрация)
        private void OnUserSignedUpRequested(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User; // Запоминаем только что созданную сущность в сессию
            CurrentMainZone = MainTab.ClientZone; // Жестко переключаем макро-рубильник холста на Клиента!
            CurrentClientTab = ClientSubTab.Messenger; // Схлопываем форму регистрации в мессенджер

            // 🔥 ПИНГ-ПОНГ НАЗАД: Выстреливаем дуэтом стейтов, чтобы сработал DataTrigger в TitleBarControl.xaml!
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        private void OnLogoutClientRequested(IUserVm.LogoutClient msg)
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None; // Выключили свет за собой

            // 🔥 ПИНГ-ПОНГ: Полный возврат на заставку
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<IUserVm.OpenAuth>(OnOpenAuthRequested);
            EventBus.Unsubscribe<IUserVm.OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Unsubscribe<IUserVm.BackToStart>(OnBackToStartRequested);
            EventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUpRequested); // Чистим память! 🛡️
            EventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }
    }
}
