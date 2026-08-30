using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services.ListUsersServise;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class MainViewModel : FormViewModelBase, IDisposable
    {
        #region Поля и свойства (Только то, что реально нужно ядру)
        private readonly ListUsersService _listUsersService;
        private readonly ILogger _logger;
        private readonly EventBus _eventBus;
        private MainWindow? _mainWindow;
        private string _statusText = "Инициализация...";
        private Visibility _panelVisibility = Visibility.Collapsed;

        public MainWindow MainWindow
        {
            get => _mainWindow ?? throw new InvalidOperationException($"{nameof(_mainWindow)} не инициализирован");
            set
            {
                if (SetField(ref _mainWindow, value) && _mainWindow != null)
                {
                    _mainWindow.LocationChanged += OnMainWindowCoordinatesChanged;
                    _mainWindow.SizeChanged += OnMainWindowCoordinatesChanged;
                }
            }
        }

        // Автоматические свойства для XAML-биндингов (заполняются через DI)
        public RegistrationViewModel? RegistrationViewModel { get; set; }
        public DeletionUsersViewModel? DeleteUsersViewModel { get; set; }
        public AuthenticationViewModel? LoginViewModel { get; set; }
        public AdminMenuViewModel? AdminMenuViewModel { get; set; }
        public LogWindow? LogWindow { get; set; }
        public LogViewModel? LogVm { get; set; }

        public EventBus EventBus => _eventBus;


        public bool IsButtonsPanelEnabled =>
            !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
            LoginViewModel?.ControlVisibility == Visibility.Visible ||
            DeleteUsersViewModel?.ControlVisibility == Visibility.Visible);

        public ObservableCollection<UserEntity> Users => _listUsersService.Users;

        public string StatusText
        {
            get => _statusText;
            private set => SetField(ref _statusText, value);
        }

        public Visibility PanelVisibility
        {
            get => _panelVisibility;
            set => SetField(ref _panelVisibility, value);
        }
        #endregion

        #region Конструктор (РАЗГРУЖЕННЫЙ: Только 3 базовые зависимости!)
        public MainViewModel(ILogger logger,EventBus eventBus,ListUsersService listUsersService)           
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _listUsersService = listUsersService ?? throw new ArgumentNullException(nameof(listUsersService));

            InitializeBusSubscriptions();
            InitializeEventHandlers();

            _ = InitializeAsync();
        }
        #endregion

        #region Инициализация подписок шины сообщений
        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<UserDeletedMessage>(async msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (userToRemove != null)
                {
                    string name = userToRemove.FirstName ?? "в имени Null";
                    Application.Current.Dispatcher.Invoke(() => Users.Remove(userToRemove));
                    await UpdateStatusText($"Пользователь {name} удален");
                }
            });

            _eventBus.Subscribe<UserAddedMessage>(async msg =>
            {
                if (msg.User == null) return;
                Application.Current.Dispatcher.Invoke(() => Users.Add(msg.User));
                await UpdateStatusText($"Пользователь {msg.User.FirstName} добавлен");
            });
        }

        private void InitializeEventHandlers()
        {
            OnFormVisibilityChanged += OnGlobalFormVisibilityChanged;
        }
        #endregion

        #region Логика работы
        private async Task InitializeAsync()
        {
            try
            {
                await _listUsersService.RefreshUsersAsync();
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await UpdateStatusText("Инициализация пользователей успешна");

                    _eventBus.Publish(new UsersListRefreshedMessage(Users));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при старте приложения: {ex.Message}");
                StatusText = "Ошибка загрузки данных при старте";
            }
        }

        public void ConnectToMainWindow(MainWindow mainWindow) => MainWindow = mainWindow;

        private void OnMainWindowCoordinatesChanged(object? sender, EventArgs e)
        {
            if (_mainWindow == null) return;

            _eventBus.Publish(new WindowPositionChangedMessage(
                _mainWindow.Left,
                _mainWindow.Top,
                _mainWindow.Width,
                _mainWindow.Height,
                _mainWindow.IsLoaded
            ));
        }

        private async Task UpdateStatusText(string text)
        {
            StatusText = "Загрузка";
            await Task.Delay(1000);
            StatusText = text;
            await Task.Delay(3000);
            StatusText = $"Загружено {Users.Count} пользователей";
        }
        #endregion

        #region Команды
        public ICommand LogoutCommand => new RelayCommand(_ =>
        {
            PanelVisibility = Visibility.Collapsed;
            OnGlobalResetRequested?.Invoke();
            _ = UpdateStatusText("Выход из аккаунта выполнен успешно");
        });

        public ICommand ToggleFormVisibilityCommand => new RelayCommand(parameter =>
        {
            var vm = parameter as FormViewModelBase;
            if (vm?.ControlVisibility != null)
            {
                if (PanelVisibility == Visibility.Visible) return;
                vm.ControlVisibility = vm.ControlVisibility == Visibility.Collapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        });

        private void OnGlobalFormVisibilityChanged(FormViewModelBase activeForm, Visibility visibility)
        {
            OnPropertyChanged(nameof(IsButtonsPanelEnabled));

            // ИСПРАВЛЕНИЕ: Используем приведенную activeForm вместо nullable-свойства DeleteUsersViewModel!
            if (activeForm is DeletionUsersViewModel deleteVm && visibility == Visibility.Visible)
            {
                deleteVm.MainUsersList = Users;
            }

            string formName = activeForm switch
            {
                DeletionUsersViewModel _ => "Удаление пользователей",
                RegistrationViewModel _ => "Регистрация",
                AuthenticationViewModel _ => "Авторизация",
                _ => "Форма"
            };

            if (visibility == Visibility.Visible)
                _ = UpdateStatusText($"Открыта форма: {formName}");
            else
                _ = UpdateStatusText($"Cold close: {formName}");
        }

        #endregion

        public void Dispose()
        {
            OnFormVisibilityChanged -= OnGlobalFormVisibilityChanged;
            if (_mainWindow != null)
            {
                _mainWindow.LocationChanged -= OnMainWindowCoordinatesChanged;
                _mainWindow.SizeChanged -= OnMainWindowCoordinatesChanged;
            }
        }
    }
}
