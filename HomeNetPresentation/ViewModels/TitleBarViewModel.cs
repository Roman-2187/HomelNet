using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 Реализуем IDisposable для полной зачистки шины событий при уничтожении компонента
    public partial class TitleBarViewModel : FormViewModelBase, IDisposable
    {
        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;

        public TitleBarViewModel(IEventBus eventBus, NavigationStateManager navigationStateManager)
            : base(eventBus, navigationStateManager)
        {
            // 🔥 ЧИСТОТА: Заменили стрелочную анонимную лямбду на ссылку на именованный метод! 🧼
            _eventBus.Subscribe<ITitleBarViewModel.ZoneChanged>(OnZoneChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnZoneChanged(ITitleBarViewModel.ZoneChanged msg)
        {
            // Синхронизируем локальные свойства шапки для триггеров XAML
            CurrentMainTab = msg.TargetTab;
            CurrentClientTab = msg.ClientTab;
        }

        #endregion

        // 🛠️ КНОПКА: Админка
        [RelayCommand]
        private void ToggleAdminZone()
        {
            if (Navigation.CurrentMainZone == MainTab.AdminZone)
                Navigation.SetStartZone();
            else
                Navigation.SetAdminZone();
        }

        // 🔑 КНОПКИ: Вход и Регистрация
        [RelayCommand]
        private void SwitchClientTab(ClientSubTab targetTab)
        {
            Navigation.SetAuthZone(targetTab);
        }

        // 🔑 КНОПКА: Выход
        [RelayCommand]
        private void Logout()
        {
            Navigation.ClearToGuest();
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Выход из аккаунта выполнен успешно"));
        }

        // ✕ КНОПКА: Закрыть приложение
        [RelayCommand]
        private void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        // 🪵 КНОПКА: Глобальный логгер
        [RelayCommand]
        private void ToggleGlobalLogger()
        {
            IsGlobalLoggerVisible = !IsGlobalLoggerVisible;

            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Переключение глобального оверлея логов..."));
            _eventBus.Publish(this, new ITitleBarViewModel.ToggleAnimation(IsGlobalLoggerVisible));
        }

        // 🔳 КНОПКА: Развернуть окно
        [RelayCommand]
        private void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Полностью снимает подписку шапки с шины событий.
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe<ITitleBarViewModel.ZoneChanged>(OnZoneChanged);
        }

        #endregion
    }
}