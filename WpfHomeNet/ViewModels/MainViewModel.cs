using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        public Action<UserEntity?>? AddUserAction { get; private set; }
        public RegistrationViewModel RegistrationViewModel { get; set; }

        public Action<int>? RemoveUserAction { get; set; }

        public DeleteUsersViewModel DeleteUsersViewModel { get; set; }

        public LoginViewModel LoginViewModel { get; set; }
        public LogWindow LogWindow { get; set; }

        public LogViewModel LogVm { get; set; }

        MainWindow? _mainWindow;

        public MainWindow MainWindow
        {
            get => _mainWindow ??
                throw new InvalidOperationException($"{nameof(_mainWindow)} не инициализирован");
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
            LogWindow logWindow, LogViewModel logView,
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


            // Модернизируем экшн удаления (делаем его асинхронным через async)
            RemoveUserAction = async (id) =>
            {
                // Ищем пользователя в твоей ObservableCollection по его ID
                var userToRemove = Users.FirstOrDefault(u => u.Id == id);

                if (userToRemove != null)
                {
                    // Сохраняем имя для красивого статуса перед тем, как стереть
                    string name = userToRemove.FirstName??"в имени Null";

                    // Удаляем из коллекции — WPF сам мгновенно уберет строку из таблицы!
                    Users.Remove(userToRemove);

                    // Запускаем твой фирменный метод обновления статус-бара с анимацией!
                    await UpdateStatusText($"Пользователь {name} удален");
                }
            };





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
                catch (Exception )
                {
                    // Логирование или уведомление пользователя
                }
            });


            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
            DeleteUsersViewModel.PropertyChanged += OnChildVmPropertyChanged; 

        }

        public void ConnectToMainWindow(MainWindow mainWindow) => MainWindow = mainWindow;


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



        private Visibility _panelVisibility = Visibility.Collapsed;
        public Visibility PanelVisibility
        {
            get => _panelVisibility;
            set => SetField(ref _panelVisibility, value);
        }

        protected bool SetField<T>(
           ref T field,
           T value,
           [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName??string.Empty);
            return true;
        }

        public ICommand ToggleFormVisibilityCommand => new RelayCommand(parameter =>
        {
            // Приводим параметр к нужному типу
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
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }


        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            private set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }


        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public void Dispose() => LogVm?.Dispose();
    }
}


