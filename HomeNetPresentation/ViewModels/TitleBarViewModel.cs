using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using HomeNetServices.Diagnostics;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Стерильная верхняя панель управления окном мессенджера SiberNet.
    /// Полная изоляция: класс общается с навигацией ИСКЛЮЧИТЕЛЬНО через рацию автобуса.
    /// </summary>
    public partial class TitleBarViewModel : FormViewModelBase<BaseNavigationState>, IDisposable
    {

    ILogger _logger;
        [ObservableProperty] private MainTab _currentMainZone = MainTab.StartZone;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
       

        public TitleBarViewModel(IEventBus eventBus,ILogger logger, BaseNavigationState navigationStateManager)
            : base(eventBus, navigationStateManager)
        {
            _eventBus.Subscribe<ITitleBarVm.MacroZoneChanged>(OnMacroZoneChanged);

            _logger = logger;
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

            _logger.LogInfo("Зашли в аутенфикацию");
        }

        // 📝 КНОПКА: Регистрация
        [RelayCommand]
        public void OpenRegisterZone()
        {
            _eventBus.Publish(this, new IUserVm.OpenRegistration());

            _logger.LogInfo("Зашли в регистрацию");
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
