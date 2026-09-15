using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces;             // Наш чистый контракт IEventBus и ILogger из Ядра 🧼
using HomeNetCore.Messaging;              // Наши чистые сигналы-рекорды из Ядра
using HomeNetCore.Models;
using HomeNetServices.Services.Messaging;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel : FormViewModelBase
    {
        #region Поля и Зависимости 🦾
        private readonly ILogger _logger;
        private readonly HashSet<Type> _openedForms = new(); // Наш умный радар окон 🧼

        // 🔥 ПОБЕДА НАД WPF: Заменили Visibility на чистый bool для Авалонии! 🧼🛸
        [ObservableProperty] private bool _isMainInterfaceVisible = false;
        [ObservableProperty] private bool _isAdminMenuVisible = false;

        // Кнопки активны, если в радаре 0 открытых окон! 🛸🛡️
        public bool IsButtonsPanelEnabled => _openedForms.Count == 0;
        #endregion

        #region Конструктор
        // Принимаем чистый интерфейс IEventBus из Ядра и передаем в базу через base(eventBus)
        public MainViewModel(ILogger logger, IEventBus eventBus) : base(eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeBusSubscriptions();

            _logger.LogInformation("test");
            _logger.LogDebug("test");
            _logger.LogWarning("test");
            _logger.LogError("test");
        }
        #endregion

        #region Инициализация подписок шины (Только глобальный интерфейс)
        private void InitializeBusSubscriptions()
        {
            // 🔥 МАКСИМАЛЬНОЕ СЖАТИЕ БЕЗ ДИСПЕТЧЕРОВ WPF: 
            // Возврат в UI-поток кроссплатформенно обеспечивает SynchronizationContext нашей шины Routing! 🧼⚡
            EventBus.Subscribe<FormVisibilityChangedMessage>(msg =>
            {
                if (msg.IsVisible)
                {
                    _openedForms.Add(msg.FormType);
                }
                else
                {
                    _openedForms.Remove(msg.FormType);
                }

                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
            });

            // Лямбда-сжатие для меню админа
            EventBus.Subscribe<AdminMenuVisibilityChangedMessage>(msg =>
                IsAdminMenuVisible = msg.IsVisible);

            // Подписка на регистрацию и авторизацию
            EventBus.Subscribe<UserLoggedMessage>(msg => OnAuthSuccess(msg.User, msg.IsFromAdminPanel));

            // В Ядре мы объединили регистрацию под UserAddedMessage 🧼
            EventBus.Subscribe<UserAddedMessage>(msg => OnAuthSuccess(msg.User, false));
        }
        #endregion

        private void OnAuthSuccess(UserEntity user, bool isFromAdminPanel)
        {
            if (user == null) return;

            if (isFromAdminPanel || IsAdminMenuVisible)
            {
                // Сценарий админа
                _openedForms.Clear();
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
                EventBus.Publish(this, new StatusTextChangedMessage($"[Админ-Режим] Успешное действие: {user.FirstName}"));
            }
            else
            {
                // 👤 СЦЕНАРИЙ ОБЫЧНОГО ЮЗЕРА
                IsMainInterfaceVisible = true;
                IsAdminMenuVisible = false;

                // 🔥 АВТОМАТИЗАЦИЯ ЧЕРЕЗ АВТОБУС (С БОЛЬШОЙ БУКВЫ):
                // Шлём в шину приказы на чистом bool: "Наглухо скрыть формы регистрации и входа с экрана!"
                EventBus.Publish(this, new FormVisibilityChangedMessage(typeof(RegistrationViewModel), false));
                EventBus.Publish(this, new FormVisibilityChangedMessage(typeof(AuthenticationViewModel), false));

                _openedForms.Clear();
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                EventBus.Publish(this, new StatusTextChangedMessage($"Добро пожаловать, {user.FirstName}!"));
            }
        }

        #region НАНО-КОМАНДЫ (Чистое управление экраном без WPF) 🛸

        [RelayCommand]
        private void ToggleAdminMenu() =>
            EventBus.Publish(this, new AdminMenuVisibilityChangedMessage(!IsAdminMenuVisible));

        [RelayCommand]
        private void Logout()
        {
            IsMainInterfaceVisible = false;
            _openedForms.Clear(); // Чистим радар окон при выходе
            OnPropertyChanged(nameof(IsButtonsPanelEnabled));

            OnGlobalResetRequested?.Invoke();
            EventBus.Publish(this, new StatusTextChangedMessage("Выход из аккаунта выполнен успешно"));
        }

        [RelayCommand]
        private void ToggleFormVisibility(object parameter)
        {
            if (parameter is FormViewModelBase vm && !IsMainInterfaceVisible)
            {
                // Переключаем чистый инвертированный bool флаг!
                vm.IsControlVisible = !vm.IsControlVisible;
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                // База FormViewModelBase сама внутри себя вызовет отправку сообщения, 
                // но для надежности дублируем по нашему исходнику
                EventBus.Publish(this, new FormVisibilityChangedMessage(vm.GetType(), vm.IsControlVisible));
            }
        }
        #endregion
    }
}
