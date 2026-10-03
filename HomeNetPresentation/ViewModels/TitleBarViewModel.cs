using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Стерильная верхняя панель управления окном мессенджера SiberNet.
    /// Полная изоляция: класс общается с навигацией ИСКЛЮЧИТЕЛЬНО через рацию автобуса.
    /// </summary>
    public partial class TitleBarViewModel : FormViewModelBase<NavigationStateManager>, IDisposable
    {
        [ObservableProperty] private MainTab _currentMainZone = MainTab.StartZone;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;

        public TitleBarViewModel(IEventBus eventBus, NavigationStateManager navigationStateManager)
            : base(eventBus, navigationStateManager)
        {
            _eventBus.Subscribe<ITitleBarVm.MacroZoneChanged>(OnMacroZoneChanged);
        }

        #region 🎧 МЕТОД ПРИЁМА ОБРАТНОГО СИГНАЛА БЭКЕНДА 🧼

        private void OnMacroZoneChanged(ITitleBarVm.MacroZoneChanged msg)
        {
            if (msg == null) return;

            CurrentMainZone = msg.ActiveZone;
            CurrentClientTab = msg.ActiveClientTab;
        }

        #endregion

        // 🛠️ КНОПКА: Админка
        [RelayCommand]
        public void ToggleAdminZone()
        {
            if (CurrentMainZone == MainTab.AdminZone)
            {
                _eventBus.Publish(this, new IAdminVm.LogoutAdmin());
            }
            else
            {
                _eventBus.Publish(this, new IAdminVm.RequestAdminZone());
            }
        }

        // 🔑 КНОПКА: Вход
        [RelayCommand]
        public void OpenAuthZone()
        {
            _eventBus.Publish(this, new IUserVm.OpenAuth());
        }

        // 📝 КНОПКА: Регистрация
        [RelayCommand]
        public void OpenRegisterZone()
        {
            _eventBus.Publish(this, new IUserVm.OpenRegistration());
        }

        // 🔑 КНОПКА: Выход из профиля клиента
        [RelayCommand]
        public void Logout()
        {
            _eventBus.Publish(this, new IUserVm.LogoutClient());
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Выход из аккаунта выполнен успешно"));
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<ITitleBarVm.MacroZoneChanged>(OnMacroZoneChanged);
        }

        #endregion
    }
}
