using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    
    public class MainViewModel : FormViewModelBase, IDisposable
    {
        public Action<UserEntity?>? AddUserAction { get; private set; }
        public Action<int>? RemoveUserAction { get; set; }

        public RegistrationViewModel RegistrationViewModel { get; set; }
        public DeleteUsersViewModel DeleteUsersViewModel { get; set; }
        public LoginInViewModel LoginViewModel { get; set; }
        public AdminMenuViewModel AdminMenuViewModel { get; }
        public LogWindow LogWindow { get; set; }
        public LogViewModel LogVm { get; set; }

        private MainWindow? _mainWindow;
        public MainWindow MainWindow
        {
            get => _mainWindow ?? throw new InvalidOperationException($"{nameof(_mainWindow)} не инициализирован");
            set => _mainWindow = value;
        }

        private ObservableCollection<UserEntity> _users = new ObservableCollection<UserEntity>();
        public ObservableCollection<UserEntity> Users
        {
            get => _users;
            set => SetField(ref _users, value);
        }

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

        private readonly UserService userService;
        private readonly ILogger logger;

        public MainViewModel(
            UserService userService,
            ILogger logger,
            RegistrationViewModel registrationVm,
            LoginInViewModel loginViewModel,
            AdminMenuViewModel adminMenuViewModel,
            LogWindow logWindow,
            LogViewModel logView,
            DeleteUsersViewModel deleteUsersViewModel)
        {
            this.userService = userService;
            this.logger = logger;
            RegistrationViewModel = registrationVm;
            LoginViewModel = loginViewModel;
            AdminMenuViewModel = adminMenuViewModel;
            LogWindow = logWindow;
            LogVm = logView;
            DeleteUsersViewModel = deleteUsersViewModel;

            // Настройка экшна удаления пользователей
            RemoveUserAction = async (id) =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == id);
                if (userToRemove != null)
                {
                    string name = userToRemove.FirstName ?? "в имени Null";

                    // Удаляем строго в UI-потоке
                    Application.Current.Dispatcher.Invoke(() => Users.Remove(userToRemove));

                    await UpdateStatusText($"Пользователь {name} удален");
                }
            };

            // Подключаем мост к дочерней форме удаления
            DeleteUsersViewModel.OnUserDeletedFromDb = RemoveUserAction;

            // Настройка экшна добавления пользователей
            AddUserAction = async (user) =>
            {
                if (user == null) return;

                Application.Current.Dispatcher.Invoke(() => _users.Add(user));

                await UpdateStatusText($"Пользователь {user.FirstName} добавлен");
            };

            // ИСПРАВЛЕНО: Убрали дубликаты! Оставляем строго одну подписку на каждую форму
            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
            DeleteUsersViewModel.PropertyChanged += OnChildVmPropertyChanged;

            // Железная подписка на сигнал пакетной заливки админки
            AdminMenuViewModel.OnDataSeeded = async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadUsersAsync();
                    await UpdateStatusText("Тестовые пользователи успешно добавлены!");
                });
            };

            // Запускаем безопасную асинхронную инициализацию данных
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadUsersAsync();
                await UpdateStatusText("Инициализация пользователей успешна");
            }
            catch (Exception)
            {
                logger.LogError("Ошибка при старте приложения");
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

            StatusText = $"Загружено {_users.Count} пользователей";
        }

        public async Task LoadUsersAsync()
        {
            var usersList = await this.userService.GetAllUsersAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Users = new ObservableCollection<UserEntity>(usersList);
            });
        }

        public ICommand LogoutCommand => new RelayCommand(_ =>
        {
            if (RegistrationViewModel != null)
            {
                RegistrationViewModel.ResetSession();
                OnPropertyChanged(nameof(RegistrationViewModel.IsComplete));
            }

            PanelVisibility = Visibility.Collapsed;
            if (DeleteUsersViewModel != null) DeleteUsersViewModel.ControlVisibility = Visibility.Collapsed;
            if (RegistrationViewModel != null) RegistrationViewModel.ControlVisibility = Visibility.Collapsed;
            if (LoginViewModel != null) LoginViewModel.ControlVisibility = Visibility.Collapsed;

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

        public bool IsButtonsPanelEnabled =>
             !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
               LoginViewModel?.ControlVisibility == Visibility.Visible ||
               DeleteUsersViewModel?.ControlVisibility == Visibility.Visible);

        private void OnChildVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ControlVisibility" || e.PropertyName == nameof(FormViewModelBase.ControlVisibility))
            {
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                if (sender is FormViewModelBase form)
                {
                    string formName = "Форма";

                    if (form.ControlVisibility == Visibility.Visible)
                    {
                        DeleteUsersViewModel.MainUsersList = this.Users;
                    }

                    if (sender is DeleteUsersViewModel)
                        formName = "Удаление пользователей";
                    else if (sender is RegistrationViewModel)
                        formName = "Регистрация";
                    else if (sender is LoginInViewModel)
                        formName = "Авторизация";

                    if (form.ControlVisibility == Visibility.Visible)
                        _ = UpdateStatusText($"Открыта форма: {formName}");
                    else
                        _ = UpdateStatusText($"Закрыта форма: {formName}");
                }
            }
        }

        public void Dispose() => LogVm?.Dispose();
    }
}



