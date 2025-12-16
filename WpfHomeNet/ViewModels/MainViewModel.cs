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
        public LogWindow LogWindow { get;  set; }
        public LoginViewModel LoginViewModel { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        private ObservableCollection<UserEntity> _users = [];
        private string _statusText = string.Empty;
        private readonly UserService userService;
        private readonly ILogger logger;
        public AdminMenuViewModel AdminMenuViewModel { get; }

        public MainViewModel(
            UserService userService,
            ILogger logger,
            RegistrationViewModel registrationVm,
            LoginViewModel loginViewModel,
            AdminMenuViewModel adminMenuViewModel,
            LogWindow logWindow)

        {
            this.userService = userService;
            this.logger = logger;
            RegistrationViewModel = registrationVm;
            LoginViewModel = loginViewModel;
            AdminMenuViewModel = adminMenuViewModel;
            LogWindow = logWindow;

            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
        }


      


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
    }
}


