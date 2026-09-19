using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel : FormViewModelBase, IMainViewModel
    {
        #region Поля и Зависимости 🦾
        private readonly ILogger _logger;

        // 🔥 НАШИ ДВА МАКРО-РУБИЛЬНИКА: Главные автоматы состояний!
        [ObservableProperty] private MainTab _currentMainTab = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.Authentication;

        // Текст для реактивного статус-бара на нижнем этаже
        [ObservableProperty] private string _statusText = "Система готова...";

        // Глобальный тумблер терминала логов
        [ObservableProperty] private bool _isGlobalLoggerVisible = false;
        #endregion

        #region Конструктор
        public MainViewModel(ILogger logger, IEventBus eventBus) : base(eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeBusSubscriptions();
        }
        #endregion

        #region Инициализация подписок шины
        private void InitializeBusSubscriptions()
        {
            // 🔥 ПОПРАВИЛИ: Слушаем новые вложенные рекорды
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg => OnAuthSuccess(msg.User, msg.IsFromAdminPanel));
            EventBus.Subscribe<IUsersTableViewModel.Added>(msg => OnAuthSuccess(msg.User, false));

            // 🔥 ВОТ ОН — НАШ НОВЫЙ ГЛАВНЫЙ ПЕРЕХВАТЧИК СИГНАЛА ИЗ АВТОБУСА!
            EventBus.Subscribe<IMainViewModel.ZoneChanged>(msg =>
            {
                _logger.LogInformation($"[АВТОБУС РОУТЕРА]: Перехватили смену зоны на {msg.TargetTab}");

                // Насильно пинаем свойство, чтобы XAML проснулся от сигнала шины
                CurrentMainTab = msg.TargetTab;
            });
        }
        #endregion

        #region Обработчик авторизации
        private void OnAuthSuccess(UserEntity user, bool isFromAdminPanel)
        {
            if (user == null) return;

            if (isFromAdminPanel)
            {
                // 👤 СЦЕНАРИЙ АДМИНА: переключаем только макро-зону в админку
                CurrentMainTab = MainTab.AdminZone;
            }
            else
            {
                // 💬 СЦЕНАРИЙ ОБЫЧНОГО ЮЗЕРА: уходим в рабочую зону клиента и врубаем мессенджер
                CurrentMainTab = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger;
            }
        }
        #endregion

        #region НАНО-КОМАНДЫ (Управление автоматом без единого флага) 🛸

        [RelayCommand]
        private void ToggleAdminZone()
        {
            // 🎛️ Перещёлкиваем макро-энум
            CurrentMainTab = CurrentMainTab == MainTab.AdminZone ? MainTab.ClientZone : MainTab.AdminZone;

            // 🔥 ПОПРАВИЛИ: Сигнал смены зоны через короткий рекорд из Ядра
            _eventBus.Publish(this, new IMainViewModel.ZoneChanged(CurrentMainTab));
        }

        [RelayCommand]
        private void SwitchClientTab(ClientSubTab targetTab)
        {
            // Команда для переключения между Входом и Регистрацией, пока пользователь не авторизован
            if (CurrentMainTab == MainTab.None)
            {
                CurrentClientTab = targetTab;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            // Сброс автомата в начальное состояние "Окно входа"
            CurrentMainTab = MainTab.None;
            CurrentClientTab = ClientSubTab.Authentication;

            OnGlobalResetRequested?.Invoke();

            // 🔥 ПОПРАВИЛИ: Короткий рекорд статус-бара
            EventBus.Publish(this, new IStatusBarViewModel.TextChanged("Выход из аккаунта выполнен успешно"));
        }

        [RelayCommand]
        private void RequestCloseApplication()
        {
            _logger.LogInformation("[АВТОМАТ]: Пользователь инициировал выход. Шлём сигнал закрытия в шину...");

            // 🔥 ПОПРАВИЛИ: WindowAnimator поймает этот чистый короткий сигнал из IMainViewModel
            EventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        [RelayCommand]
        private void ToggleGlobalLogger()
        {
            IsGlobalLoggerVisible = !IsGlobalLoggerVisible;
            _logger.LogInformation($"[СИСТЕМА]: Глобальный оверлей логов переключен. Статус: {IsGlobalLoggerVisible}");

            // 🔥 ПОПРАВИЛИ: Публикуем короткую команду анимации логгера
            EventBus.Publish(this, new IAdminMenuViewModel.ToggleAnimation(this));
        }

        [RelayCommand]
        private void ToggleGrowWindow()
        {
            _logger.LogInformation("[АВТОМАТ]: Запущен триггер плавного киберпанк-вырастания окна.");

            // 🔥 ПОПРАВИЛИ: Сигнал киберпанк-вырастания окна
            EventBus.Publish(this, new IMainViewModel.ToggleSize());
        }
        #endregion
    }
}
