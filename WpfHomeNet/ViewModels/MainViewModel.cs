using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class MainViewModel : FormViewModelBase
    {
        #region Поля и Зависимости 🦾
        private readonly ILogger _logger;
        private readonly HashSet<Type> _openedForms = new(); // Наш умный радар окон 🧼

        [ObservableProperty] private Visibility _mainInterfaceVisibility = Visibility.Collapsed;
        [ObservableProperty] private Visibility _adminMenuVisibility = Visibility.Collapsed;

        // Стрелка заменяет get, return и скобки! Датчик считает всё через Any() 👌
        // Датчик считает всё через Count. Кнопки активны, если в радаре 0 открытых окон! 🛸🛡️
        public bool IsButtonsPanelEnabled => _openedForms.Count == 0;

        #endregion

        #region Конструктор
        public MainViewModel(ILogger logger, EventBus eventBus) : base(eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeBusSubscriptions();
        }
        #endregion

        #region Инициализация подписок шины (Только глобальный интерфейс)
        private void InitializeBusSubscriptions()
        {
            // ГИПЕРСЖАТИЕ: тернарник рулит логикой добавления/удаления в одну строку! 🛸
            _eventBus.Subscribe<FormVisibilityChangedMessage>(msg => Application.Current.Dispatcher.Invoke(() =>
            {
                _ = msg.Visibility == Visibility.Visible ? _openedForms.Add(msg.FormType) : _openedForms.Remove(msg.FormType);

                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
            }));

            // Лямбда-сжатие для меню админа
            _eventBus.Subscribe<AdminMenuVisibilityChangedMessage>(msg =>
                AdminMenuVisibility = msg.IsVisible ? Visibility.Visible : Visibility.Collapsed);

            _eventBus.Subscribe<UserRegisteredMessage>(msg =>
                 Application.Current.Dispatcher.Invoke(() => OnAuthSuccess(msg.User, msg.IsFromAdminPanel)));

            _eventBus.Subscribe<UserLoggedMessage>(msg =>
                Application.Current.Dispatcher.Invoke(() => OnAuthSuccess(msg.User, msg.IsFromAdminPanel)));
        }
        #endregion

        private void OnAuthSuccess(UserEntity user, bool isFromAdminPanel)
        {
            if (user == null) return;

            if (isFromAdminPanel || AdminMenuVisibility == Visibility.Visible)
            {
                // Сценарий админа (оставляем как есть)
                _openedForms.Clear();
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
                _eventBus.Publish(new StatusTextChangedMessage($"[Админ-Режим] Успешное действие: {user.FirstName}"));
            }
            else
            {
                // 👤 СЦЕНАРИЙ ОБЫЧНОГО ЮЗЕРА
                MainInterfaceVisibility = Visibility.Visible;
                AdminMenuVisibility = Visibility.Collapsed;

                // 🔥 ВОТ ОНА — АВТОМАТИЗАЦИЯ ЧЕРЕЗ АВТОБУС!
                // Шлём в шину приказы: "Наглухо скрыть формы регистрации и входа с экрана!"
                _eventBus.Publish(new FormVisibilityChangedMessage(typeof(RegistrationViewModel), Visibility.Collapsed));
                _eventBus.Publish(new FormVisibilityChangedMessage(typeof(AuthenticationViewModel), Visibility.Collapsed));

                _openedForms.Clear();
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                _eventBus.Publish(new StatusTextChangedMessage($"Добро пожаловать, {user.FirstName}!"));
            }
        }




        #region НАНО-КОМАНДЫ (Чистое управление экраном) 🛸

        // void-метод на лямбда-стрелке. Читается как чистая декларация намерения! 💎
        [RelayCommand]
        private void ToggleAdminMenu() =>
            _eventBus.Publish(new AdminMenuVisibilityChangedMessage(AdminMenuVisibility != Visibility.Visible));

        [RelayCommand]
        private void Logout()
        {
            MainInterfaceVisibility = Visibility.Collapsed;
            _openedForms.Clear(); // Чистим радар окон при выходе
            OnPropertyChanged(nameof(IsButtonsPanelEnabled));

            OnGlobalResetRequested?.Invoke();
            _eventBus.Publish(new StatusTextChangedMessage("Выход из аккаунта выполнен успешно"));
        }

        [RelayCommand]
        private void ToggleFormVisibility(object parameter)
        {
            if (parameter is FormViewModelBase vm && MainInterfaceVisibility != Visibility.Visible)
            {
                vm.ControlVisibility = vm.ControlVisibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
                _eventBus.Publish(new FormVisibilityChangedMessage(vm.GetType(), vm.ControlVisibility));
            }
        }
        #endregion
    }
}
