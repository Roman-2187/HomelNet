using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services.ListUsersServise;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class MainViewModel : FormViewModelBase
    {
        #region Поля и свойства (Только чистые данные)
        private readonly ListUsersService _listUsersService;
        private readonly ILogger _logger;
        private readonly EventBus _eventBus;
        private Visibility _panelVisibility = Visibility.Collapsed;
        private Visibility _adminMenuVisibility = Visibility.Collapsed;
        public RegistrationViewModel? RegistrationViewModel { get; set; }
        public DeletionUsersViewModel? DeleteUsersViewModel { get; set; }
        public AuthenticationViewModel? LoginViewModel { get; set; }
        public AdminMenuViewModel? AdminMenuViewModel { get; set; }
        public LogWindow? LogWindow { get; set; }
        public LogViewModel? LogVm { get; set; }

        // Свойство для нашего выселенного статус-бара
        public StatusBarViewModel? StatusBarViewModel { get; set; }

        public RelayCommand ToggleAdminMenuCommand { get; }
        public EventBus EventBus => _eventBus;
        public ObservableCollection<UserEntity> Users => _listUsersService.Users;

        public bool IsButtonsPanelEnabled =>
            !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
            LoginViewModel?.ControlVisibility == Visibility.Visible ||
            DeleteUsersViewModel?.ControlVisibility == Visibility.Visible);

        public Visibility PanelVisibility
        {
            get => _panelVisibility;
            set => SetField(ref _panelVisibility, value);
        }

        public Visibility AdminMenuVisibility
        {
            get => _adminMenuVisibility;
            set => SetField(ref _adminMenuVisibility, value);
        }
        #endregion

        #region Конструктор
        public MainViewModel(ILogger logger, EventBus eventBus, ListUsersService listUsersService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _listUsersService = listUsersService ?? throw new ArgumentNullException(nameof(listUsersService));

            _adminMenuVisibility = Visibility.Collapsed;

            ToggleAdminMenuCommand = new RelayCommand(_ =>
            {
                bool isCurrentlyVisible = AdminMenuVisibility == Visibility.Visible;
                _eventBus.Publish(new AdminMenuVisibilityChangedMessage(!isCurrentlyVisible));
            });

            ToggleAdminMenuCommand = new RelayCommand(_ =>
            {
                bool isCurrentlyVisible = AdminMenuVisibility == Visibility.Visible;
                _eventBus.Publish(new AdminMenuVisibilityChangedMessage(!isCurrentlyVisible));
            });

            InitializeBusSubscriptions();
            _ = InitializeAsync();
        }
        #endregion

        #region Инициализация подписок шины (Только работа с коллекцией!)
        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (userToRemove != null)
                {
                    Application.Current.Dispatcher.Invoke(() => Users.Remove(userToRemove));
                }
            });

            _eventBus.Subscribe<AdminMenuVisibilityChangedMessage>(msg =>
            {
                AdminMenuVisibility = msg.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            });

            _eventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User == null) return;
                Application.Current.Dispatcher.Invoke(() => Users.Add(msg.User));
            });
        }
        #endregion

        #region Логика работы
        private async Task InitializeAsync()
        {
            try
            {
                await _listUsersService.RefreshUsersAsync();
                _eventBus.Publish(new UsersListRefreshedMessage(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при старте приложения: {ex.Message}");
            }
        }
        #endregion

        #region Команды
        public ICommand LogoutCommand => new RelayCommand(_ =>
        {
            PanelVisibility = Visibility.Collapsed;
            OnGlobalResetRequested?.Invoke();
            // Сигнализируем в воздух, статус-бар сам поймает!
            _eventBus.Publish(new StatusTextChangedMessage("Выход из аккаунта выполнен успешно"));
        });

        public ICommand ToggleFormVisibilityCommand => new RelayCommand(parameter =>
        {
            if (parameter is FormViewModelBase vm)
            {
                if (PanelVisibility == Visibility.Visible) return;

                vm.ControlVisibility = vm.ControlVisibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                _eventBus.Publish(new FormVisibilityChangedMessage(vm.GetType(), vm.ControlVisibility));
            }
        });
        #endregion
    }
}