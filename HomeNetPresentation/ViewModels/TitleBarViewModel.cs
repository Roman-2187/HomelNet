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
        // 🎛️ Наш единственный локальный рубильник для триггеров видимости самой шапки
        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;

        // 🎛️ Локальная копия суб-таба для триггеров главного окна
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;

        public TitleBarViewModel(IEventBus eventBus, NavigationStateManager navigationStateManager)
      : base(eventBus, navigationStateManager)
        {
            // 🎯 СЛУХОВОЙ АППАРАТ ШАПКИ: Слушаем автобус, когда дочерние формы (Регистрация/Вход)
            // нажимают "Отмена" и просят принудительно очистить экран!
            _eventBus.Subscribe<IMainViewModel.ZoneChanged>(msg =>
            {
                // Сбрасываем макро-зону шапки в None
                CurrentMainTab = msg.TargetTab;

                // Если система попросила уйти в полный ноль (None) — гасим локальный саб-таб,
                // чтобы триггер в MainWindow.xaml мгновенно схлопнул форму с экрана!
                if (msg.TargetTab == MainTab.None)
                {
                    CurrentClientTab = ClientSubTab.None;
                }
            });
        }


        // 🛠️ КНОПКА: Админка (Умный переключатель)
        [RelayCommand]
        private void ToggleAdminZone()
        {
            if (Navigation.CurrentMainZone == MainTab.AdminZone)
            {
                // Если мы уже в админке — сбрасываем приложение в стартовый ноль
                Navigation.SetStartZone();
            }
            else
            {
                // Если нет — переключаем автомат в режим админки
                Navigation.SetAdminZone();
            }

            // Синхронизируем локальный энум, чтобы в шапке спрятались/появились кнопки
            CurrentMainTab = Navigation.CurrentMainZone;
            CurrentClientTab = Navigation.CurrentClientTab;
        }

        // 🔑 КНОПКИ: Вход и Регистрация (Оживили метод!)
        [RelayCommand]
        private void SwitchClientTab(ClientSubTab targetTab)
        {
            // 🎯 МАГИЯ ТУТ: Жестко и синхронно даем команду автомату включить нужную зону!
            Navigation.SetAuthZone(targetTab);

            // Синхронизируем локальные свойства, чтобы триггеры в MainWindow мгновенно проснулись!
            CurrentMainTab = Navigation.CurrentMainZone;   // Станет None
            CurrentClientTab = Navigation.CurrentClientTab; // Станет Authentication или Registration
        }

        // ↩️ КНОПКА: Выход
        [RelayCommand]
        private void Logout()
        {
            // Чистим сессию гостя/юзера через наш манипулятор
            Navigation.ClearToGuest();

            // Синхронизируем состояние с шапкой
            CurrentMainTab = Navigation.CurrentMainZone;
            CurrentClientTab = Navigation.CurrentClientTab;

            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Выход из аккаунта выполнен успешно"));
        }

        // ✕ КНОПКА: Закрыть приложение
        [RelayCommand]
        private void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        // ⚡ КНОПКА: Дебаг / Логгер оверлей
        [RelayCommand]
        private void ToggleGlobalLogger()
        {
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Переключение глобального оверлея логов..."));
        }

        // 🔳 КНОПКА: Развернуть окно
        [RelayCommand]
        private void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }
    }
}
