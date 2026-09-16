using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Events;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel : FormViewModelBase
    {
        #region Поля и Зависимости 🦾
        private readonly ILogger _logger;

        // 🔥 НАШИ ТРИ КИТА: Главные рубильники автоматов состояний!
        [ObservableProperty] private MainTab _currentMainTab = MainTab.AuthZone;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.Authentication;
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.LogsView;

        // Дополнительные логические свойства для модулей панели администратора
        public bool IsLoggerModuleActive => CurrentMainTab == MainTab.AdminZone && CurrentAdminTab == AdminSubTab.LogsView;
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
            // Ловим успешный вход или успешную регистрацию из шины событий
            EventBus.Subscribe<UserLoggedMessage>(msg => OnAuthSuccess(msg.User, msg.IsFromAdminPanel));
            EventBus.Subscribe<UserAddedMessage>(msg => OnAuthSuccess(msg.User, false));
            EventBus.Subscribe<StatusTextChangedMessage>(msg => _logger.LogInformation($"[СТАТУС]: {msg.NewStatus}"));
        }
        #endregion

        private void OnAuthSuccess(UserEntity user, bool isFromAdminPanel)
        {
            if (user == null) return;

            if (isFromAdminPanel)
            {
                // 🛠️ СЦЕНАРИЙ АДМИНА: переключаем макро-зону в админку, открываем логи
                CurrentMainTab = MainTab.AdminZone;
                CurrentAdminTab = AdminSubTab.LogsView;

                OnPropertyChanged(nameof(IsLoggerModuleActive));
                EventBus.Publish(this, new StatusTextChangedMessage($"[Админ-Режим] Активен: {user.FirstName}"));
            }
            else
            {
                // 👤 СЦЕНАРИЙ ОБЫЧНОГО ЮЗЕРА: уходим в рабочую зону клиента и врубаем мессенджер!
                CurrentMainTab = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger;

                EventBus.Publish(this, new StatusTextChangedMessage($"Добро пожаловать в SiberNet, {user.FirstName}!"));
            }
        }

        #region НАНО-КОМАНДЫ (Управление автоматом без единого флага) 🛸

        [RelayCommand]
        private void ToggleAdminZone()
        {
            // Если мы уже в админке — возвращаемся в рабочую зону, иначе — заходим в админку
            CurrentMainTab = CurrentMainTab == MainTab.AdminZone ? MainTab.ClientZone : MainTab.AdminZone;
            OnPropertyChanged(nameof(IsLoggerModuleActive));

            EventBus.Publish(this, new StatusTextChangedMessage($"Переключение зоны. Текущая макро-зона: {CurrentMainTab}"));
        }

        [RelayCommand]
        private void SwitchClientTab(ClientSubTab targetTab)
        {
            // Команда для переключения между Входом и Регистрацией, пока пользователь не авторизован
            if (CurrentMainTab == MainTab.AuthZone)
            {
                CurrentClientTab = targetTab;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            // Сброс автомата в начальное состояние "Окно входа"
            CurrentMainTab = MainTab.AuthZone;
            CurrentClientTab = ClientSubTab.Authentication;

            OnPropertyChanged(nameof(IsLoggerModuleActive));
            OnGlobalResetRequested?.Invoke();
            EventBus.Publish(this, new StatusTextChangedMessage("Выход из аккаунта выполнен успешно"));
        }
        #endregion
    }
}
