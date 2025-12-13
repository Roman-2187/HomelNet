using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Interfaces;

namespace WpfHomeNet.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IStatusUpdater
    {
        public RegistrationViewModel RegistrationViewModel { get; private set; }
        public LoginViewModel LoginViewModel { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        private ObservableCollection<UserEntity> _users = new();
        private string _statusText = string.Empty;
        private readonly UserService userService;
        private readonly ILogger logger;

        public MainViewModel
            (
              UserService userService,
              ILogger logger,
              RegistrationViewModel registrationVm,
              LoginViewModel loginViewModel
            )

        {
            this.userService = userService;
            this.logger = logger;
            RegistrationViewModel = registrationVm;
            LoginViewModel = loginViewModel;


            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
        }





        public ICommand ShowRegistrationCommand => new RelayCommand(_ =>
        {
            if (RegistrationViewModel != null)
            {
                RegistrationViewModel.ControlVisibility =
                    RegistrationViewModel.ControlVisibility == Visibility.Collapsed
                        ? Visibility.Visible
                        : Visibility.Collapsed;               
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
            }
        });

        public ICommand ShowLoginCommand => new RelayCommand(_ =>
        {
            if (LoginViewModel != null)
            {
                LoginViewModel.ControlVisibility =
                    LoginViewModel.ControlVisibility == Visibility.Collapsed
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));
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



        public ObservableCollection<UserEntity> Users
        {
            get => _users;

            set
            {
                if (_users == value) return;
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }


        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText == value) return;
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }



        public void SetStatus(string message) => StatusText = message;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public async Task LoadUsersAsync()
        {
            StatusText = "Загрузка...";

            try
            {
                var users = await userService.GetAllUsersAsync();

                if (users == null)
                {
                    HandleError("Получены пустые данные");
                    return;
                }

                Users.Clear();
                Users = new ObservableCollection<UserEntity>(users);

                var userCount = users.Count;

                if (userCount == 0)
                {
                    HandleSuccess($"Список пользователей пуст");
                }
                else
                {
                    HandleSuccess($"Загружено {userCount} пользователей");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void HandleSuccess(string message)
        {
            logger?.LogInformation(message);
            StatusText = message;
        }

        private void HandleError(string message)
        {
            logger?.LogError(message);
            StatusText = message;
        }
    }
}

