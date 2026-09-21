using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class TitleBarViewModel : FormViewModelBase
    {
        // 🎛️ Рубильники для триггеров видимости внутри TitleBarControl.xaml
        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;

        public TitleBarViewModel(IEventBus eventBus, NavigationStateManager navigationStateManager): base(eventBus, navigationStateManager)
      
        {
            // 🎯 Слушаем автобус через твой новый интерфейс!
            _eventBus.Subscribe<ITitleBarViewModel.ZoneChanged>(msg =>
            {
                // Синхронизируем локальные свойства шапки для триггеров XAML
                CurrentMainTab = msg.TargetTab;
                CurrentClientTab = msg.ClientTab;
            });
            
        }

    

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

        // ↩️ КНОПКА: Выход
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

       

        [RelayCommand]
        private void ToggleGlobalLogger()
        {
            // 1. Инвертируем локальный бул
            IsGlobalLoggerVisible = !IsGlobalLoggerVisible;

            // 2. Пишем в статус-бар как раньше
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Переключение глобального оверлея логов..."));

            // 3. Стреляем новым чистым сообщением и передаем ТЕКУЩЕЕ состояние булла
            _eventBus.Publish(this, new ITitleBarViewModel.ToggleAnimation(IsGlobalLoggerVisible));
        }



        // 🔳 КНОПКА: Развернуть окно
        [RelayCommand]
        private void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }
    }
}
