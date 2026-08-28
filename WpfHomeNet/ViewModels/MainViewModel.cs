using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HomeNetCore.Services.ListUsersServise;

    namespace WpfHomeNet.ViewModels
    {
        public class MainViewModel : FormViewModelBase, IDisposable
        {
            public MainWindow MainWindow
            {
                get => _mainWindow ?? throw new InvalidOperationException($"{nameof(_mainWindow)} не инициализирован");
                set => _mainWindow = value;
            }
            private readonly ListUsersService _listUsersService; 
            private readonly ILogger logger;

            public Action<UserEntity?>? AddUserAction { get; private set; }
            public Action<int>? RemoveUserAction { get; set; }

            public RegistrationViewModel RegistrationViewModel { get; set; }
            public DeleteUsersViewModel DeleteUsersViewModel { get; set; }
            public LoginInViewModel LoginViewModel { get; set; }
            public AdminMenuViewModel AdminMenuViewModel { get; }
            public LogWindow LogWindow { get; set; }
            public LogViewModel LogVm { get; set; }

            private MainWindow? _mainWindow;
            

            public bool IsButtonsPanelEnabled =>
                !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
                LoginViewModel?.ControlVisibility == Visibility.Visible ||
                DeleteUsersViewModel?.ControlVisibility == Visibility.Visible);
            public ObservableCollection<UserEntity> Users => _listUsersService.Users;

            private string _statusText = "Инициализация...";
            public string StatusText
            {
                get => _statusText;
                private set => SetField(ref _statusText, value);
            }

            private Visibility _panelVisibility = Visibility.Collapsed;
            public Visibility PanelVisibility
            {
                get => _panelVisibility;
                set => SetField(ref _panelVisibility, value);
            }

            public MainViewModel(
                ILogger logger,
                RegistrationViewModel registrationVm,
                LoginInViewModel loginViewModel,
                AdminMenuViewModel adminMenuViewModel,
                LogWindow logWindow,
                LogViewModel logView,
                DeleteUsersViewModel deleteUsersViewModel,
                ListUsersService listUsersService) // Добавили деталь
            {
                this.logger = logger;
                RegistrationViewModel = registrationVm;
                LoginViewModel = loginViewModel;
                AdminMenuViewModel = adminMenuViewModel;
                LogWindow = logWindow;
                LogVm = logView;
                DeleteUsersViewModel = deleteUsersViewModel;
                _listUsersService = listUsersService ??
                throw new ArgumentNullException(nameof(listUsersService));

                // Настройка экшна удаления пользователей
                RemoveUserAction = async (id) =>
                {
                    var userToRemove = Users.FirstOrDefault(u => u.Id == id);
                    if (userToRemove != null)
                    {
                        string name = userToRemove.FirstName ?? "в имени Null";

                        // Контролируем потоки UI строго на стороне WPF!
                        Application.Current.Dispatcher.Invoke(() => Users.Remove(userToRemove));
                        await UpdateStatusText($"Пользователь {name} удален");
                    }
                };

                DeleteUsersViewModel.OnUserDeletedFromDb = RemoveUserAction;

                // Настройка экшна добавления пользователей
                AddUserAction = async (user) =>
                {
                    if (user == null) return;

                    // Контролируем потоки UI строго на стороне WPF!
                    Application.Current.Dispatcher.Invoke(() => Users.Add(user));
                    await UpdateStatusText($"Пользователь {user.FirstName} добавлен");
                };

                OnFormVisibilityChanged += OnGlobalFormVisibilityChanged;

                AdminMenuViewModel.OnDataSeeded = async () =>
                {
                    // Заливка тестовых юзеров админки теперь дергает сервис ядра!
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await _listUsersService.RefreshUsersAsync();
                        await UpdateStatusText("Тестовые пользователи успешно добавлены!");
                    });
                };

                _ = InitializeAsync();
            }

            private async Task InitializeAsync()
            {
                try
                {              
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await _listUsersService.RefreshUsersAsync();
                    });

                    await UpdateStatusText("Инициализация пользователей успешна");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Ошибка при старте приложения: {ex.Message}");
                    StatusText = "Ошибка загрузки данных при старте";
                }
            }

            public void ConnectToMainWindow(MainWindow mainWindow) => MainWindow = mainWindow;

            private async Task UpdateStatusText(string text)
            {
                StatusText = "Загрузка";
                await Task.Delay(1000);
                StatusText = text;
                await Task.Delay(3000);
                StatusText = $"Загружено {Users.Count} пользователей";
            }

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

                if (activeForm is DeleteUsersViewModel && visibility == Visibility.Visible)
                {
                    DeleteUsersViewModel.MainUsersList = this.Users;
                }

                string formName = activeForm switch
                {
                    DeleteUsersViewModel _ => "Удаление пользователей",
                    RegistrationViewModel _ => "Регистрация",
                    LoginInViewModel _ => "Авторизация",
                    _ => "Форма"
                };

                if (visibility == Visibility.Visible)
                    _ = UpdateStatusText($"Открыта форма: {formName}");
                else
                    _ = UpdateStatusText($"Cold close: {formName}");
            }
            public void Dispose()
            {
                OnFormVisibilityChanged -= OnGlobalFormVisibilityChanged;
                LogVm?.Dispose();
            }
        }
    } 