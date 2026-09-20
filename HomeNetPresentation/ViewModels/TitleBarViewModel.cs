using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using System;

namespace HomeNetPresentation.ViewModels
{
    public partial class TitleBarViewModel : FormViewModelBase
    {
        

        // Синхронизируем состояние макро-вкладки для триггеров видимости в XAML шапки
        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;

        public TitleBarViewModel(IEventBus eventBus,NavigationStateManager navigationStateManager):base(eventBus,navigationStateManager)
        {
           

            // Слушаем шину событий: если где-то в системе (логин, роутер) изменилась зона — шапка переключает свои триггеры!
            _eventBus.Subscribe<IMainViewModel.ZoneChanged>(msg =>
            {
                CurrentMainTab = msg.TargetTab;
            });
        }

        [RelayCommand]
        private void ToggleAdminZone()
        {
            // Переключаем режим тумблера админки
            var nextTab = CurrentMainTab == MainTab.AdminZone ? MainTab.ClientZone : MainTab.AdminZone;
            _eventBus.Publish(this, new IMainViewModel.ZoneChanged(nextTab));
        }

        [RelayCommand]
        private void SwitchClientTab(ClientSubTab targetTab)
        {
            // Бросаем сигнал смены вкладки (Вход/Регистрация) для левого шлюза
            // Создадим для этого короткий рекорд роутинга саб-вкладок, если нужно, либо используем существующую логику
            // Для совместимости пустим сигнал, который поймает MainViewModel или шлюз
        }

        [RelayCommand]
        private void Logout()
        {
            // Сигнал тотального сброса улетает в шину
            _eventBus.Publish(this, new IMainViewModel.ZoneChanged(MainTab.None));
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Выход из аккаунта выполнен успешно"));
        }

        [RelayCommand]
        private void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        [RelayCommand]
        private void ToggleGlobalLogger()
        {
            // Сигнал анимации логгера (раньше передавали саму MainViewModel, теперь передаем чистый сигнал)
            // Обновим рекорд подписки аниматора, чтобы он дергал булы оверлея
        }

        [RelayCommand]
        private void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }
    }
}
