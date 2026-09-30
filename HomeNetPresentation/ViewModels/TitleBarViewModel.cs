using System;
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
    /// Полная изоляция: класс общается с навигацией ИСКЛЮЧИТЕЛЬНО через рацию автобуса,
    /// принимая дуэт макро-параметров обратно для триггеров XAML.
    /// </summary>
    public partial class TitleBarViewModel : FormViewModelBase<NavigationStateManager>, IDisposable
    {
        // 🔥 ИСПРАВИЛИ: Никакого хардкода AdminZone на старте! Начинаем строго с чистого нуля.
        [ObservableProperty] private MainTab _currentMainZone = MainTab.StartZone;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;

        public TitleBarViewModel(IEventBus eventBus, NavigationStateManager navigationStateManager)
            : base(eventBus, navigationStateManager)
        {
            // 🔥 СИНХРОНИЗАЦИЯ: Подписываемся строго на наш новый сквозной макро-рекорд
            _eventBus.Subscribe<ITitleBarVm.MacroZoneChanged>(OnMacroZoneChanged);
        }

        #region 🎧 МЕТОД ПРИЁМА ОБРАТНОГО СИГНАЛА БЭКЕНДА переключения главных экранов 🧼

        private void OnMacroZoneChanged(ITitleBarVm.MacroZoneChanged msg)
        {
            if (msg == null) return;

            // 🔥 ПИНГ-ПОНГ: Раскладываем прилетевший обратно дуэт энумов по UI-свойствам
            CurrentMainZone = msg.ActiveZone;
            CurrentClientTab = msg.ActiveClientTab;
        }

        #endregion

        // 🛠️ КНОПКА: Админка (Чистый, безотказный тумблер на основе локального засинхроненного стейта)
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

        // 🔑 КНОПКА: Вход (Изолированная нано-команда PUBLIC без ломающих параметров)
        [RelayCommand]
        public void OpenAuthZone()
        {
            _eventBus.Publish(this, new IUserVm.OpenAuth());
        }

        // 📝 КНОПКА: Регистрация (Изолированная нано-команда PUBLIC без ломающих параметров)
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

        // ✕ КНОПКА: Закрыть приложение
        [RelayCommand]
        public void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }
   

        // 🔳 КНОПКА: Развернуть окно
        [RelayCommand]
        public void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
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
