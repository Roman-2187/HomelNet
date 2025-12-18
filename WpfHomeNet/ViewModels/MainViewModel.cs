using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        public Action<UserEntity?>? AddUserAction { get; private set; }   
        public RegistrationViewModel RegistrationViewModel { get; set; }
        public LoginViewModel LoginViewModel { get; set; }
        public LogWindow LogWindow { get; set; }

        public LogViewModel LogVm { get; set; }

        MainWindow? _mainWindow;

        public MainWindow MainWindow
        {
            get => _mainWindow ?? throw new InvalidOperationException($"{nameof(_mainWindow)} не инициализирован");
            set => _mainWindow = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private ObservableCollection<UserEntity> _users = new ObservableCollection<UserEntity>();

        private readonly UserService userService;
        private readonly ILogger logger;
        public AdminMenuViewModel AdminMenuViewModel { get; }

        public MainViewModel(
            UserService userService,
            ILogger logger,
            RegistrationViewModel registrationVm,
            LoginViewModel loginViewModel,
            AdminMenuViewModel adminMenuViewModel,
            LogWindow logWindow, LogViewModel logView)

        {
            this.userService = userService;
            this.logger = logger;
            RegistrationViewModel = registrationVm;
            LoginViewModel = loginViewModel;
            AdminMenuViewModel = adminMenuViewModel;
            LogWindow = logWindow;
            LogVm = logView;


            AddUserAction = async (user) =>
            {
                if (user == null) return;

                _users.Add(user);

                await UpdateStatusText($"Пользователь {user.FirstName} добавлен");

            };

            Task.Run(async () =>
            {
                try
                {
                    await LoadUsersAsync();

                    await UpdateStatusText("инициализация пользователей");

                }
                catch (Exception ex)
                {
                    // Логирование или уведомление пользователя
                }
            });


            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;


        }


        private ObservableCollection<UserEntity> Users
        {
            get => _users;
            set
            {
                if (_users == value) return;
                _users = value;
                OnPropertyChanged(nameof(Users));


            }
        }

        private async Task UpdateStatusText(string text)
        {
            StatusText = "Загрузка";

            await Task.Delay(1000);

            StatusText = text;

            await Task.Delay(1500);

            StatusText = $"Загружено {_users.Count} пользователей";
        }

        private async Task LoadUsersAsync()
        {
            var usersList = await this.userService.GetAllUsersAsync();
            Users = new ObservableCollection<UserEntity>(usersList);
        }

        public void ConnectToMainWindow(MainWindow mainWindow) => MainWindow = mainWindow;


        public void ToggleLogWindow()
        {
            if (LogVm.IsVisible)
            {
                LogVm.Hide();
            }

            else
            {
                LogVm.PositionLogWindow();
                LogVm.Show();
            }

        }

        public void Dispose() => LogVm?.Dispose();



        public ICommand ToggleFormVisibilityCommand => new RelayCommand(parameter =>
        {
            // Приводим параметр к нужному типу
            var vm = parameter as FormViewModelBase;

            if (vm?.ControlVisibility != null)
            {
                vm.ControlVisibility = vm.ControlVisibility == Visibility.Collapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        });



        public bool IsButtonsPanelEnabled =>
              !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
                LoginViewModel?.ControlVisibility == Visibility.Visible);


        private void OnChildVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RegistrationViewModel.ControlVisibility) or
                                   nameof(LoginViewModel.ControlVisibility))
            {
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
            }
        }



        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            private set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }








        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


