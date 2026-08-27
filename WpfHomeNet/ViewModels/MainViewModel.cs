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
   

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public Action<UserEntity?>? AddUserAction { get; private set; }
        public Action<int>? RemoveUserAction { get; set; }

        public RegistrationViewModel RegistrationViewModel { get; set; }
        public DeleteUsersViewModel DeleteUsersViewModel { get; set; }
        public LoginViewModel LoginViewModel { get; set; }
        public AdminMenuViewModel AdminMenuViewModel { get; }
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
        public ObservableCollection<UserEntity> Users
        {
            get => _users;
            set => SetField(ref _users, value); // Безопасное обновление через SetField
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
            LoginViewModel loginViewModel,
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

            // Подписываемся на изменения свойств дочерних окон
            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
            DeleteUsersViewModel.PropertyChanged += OnChildVmPropertyChanged;


            // ... твои старые подписки в конце конструктора MainViewModel:
            RegistrationViewModel.PropertyChanged += OnChildVmPropertyChanged;
            LoginViewModel.PropertyChanged += OnChildVmPropertyChanged;
            DeleteUsersViewModel.PropertyChanged += OnChildVmPropertyChanged;

            // ДОБАВЛЯЕМ СЮДА ЖЕЛЕЗНУЮ ПОДПИСКУ НА КНОПКУ АДМИНКИ:
            AdminMenuViewModel.OnDataSeeded = async () =>
            {
                // Возвращаемся в UI-поток WPF, чтобы безопасно перерисовать таблицу
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // 1. Просим главную модель заново перечитать базу данных
                    await LoadUsersAsync();

                    // 2. Включаем красивую цепочку анимации твоего статус-бара
                    await UpdateStatusText("Тестовые пользователи успешно добавлены!");
                });
            };
         // конец конструктора


        // Запускаем безопасную асинхронную инициализацию данных
        _ = InitializeAsync();
        }

        // Безопасный запуск первичной загрузки данных при старте программы
        private async Task InitializeAsync()
        {
            try
            {
                await LoadUsersAsync();
                await UpdateStatusText("Инициализация пользователей успешна");
            }
            catch (Exception )
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

            // Передаем коллекцию в UI поток безопасно для WPF
            Application.Current.Dispatcher.Invoke(() =>
            {
                Users = new ObservableCollection<UserEntity>(usersList);
            });
        }

        public ICommand LogoutCommand => new RelayCommand(_ =>
        {
            // 1. Сбрасываем флаг успешного входа в RegistrationViewModel (или где он у тебя хранится)
            if (RegistrationViewModel != null)
            {
                RegistrationViewModel.ResetSession();
                // Пинаем интерфейс, чтобы скрылась кнопка Выхода и вернулось меню Входа/Регистрации
                OnPropertyChanged(nameof(RegistrationViewModel.IsComplete));
            }

            // 2. Схлопываем видимость всех панелей и таблиц обратно в Collapsed
            PanelVisibility = Visibility.Collapsed;
            if (DeleteUsersViewModel != null) DeleteUsersViewModel.ControlVisibility = Visibility.Collapsed;
            if (RegistrationViewModel != null) RegistrationViewModel.ControlVisibility = Visibility.Collapsed;
            if (LoginViewModel != null) LoginViewModel.ControlVisibility = Visibility.Collapsed;

            // 3. Выводим красивый статус на прощание
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

        // Панель кнопок блокируется, если открыта ЛЮБАЯ из трех форм
        public bool IsButtonsPanelEnabled =>
             !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
               LoginViewModel?.ControlVisibility == Visibility.Visible ||
               DeleteUsersViewModel?.ControlVisibility == Visibility.Visible);

        private void OnChildVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Ловим изменение видимости у любой формы
            if (e.PropertyName == "ControlVisibility" || e.PropertyName == nameof(FormViewModelBase.ControlVisibility))
            {
                // Обновляем доступность кнопок на главной панели
                OnPropertyChanged(nameof(IsButtonsPanelEnabled));

                // Заставляем статус-бар говорить, какая именно форма открылась/закрылась
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
                    else if (sender is LoginViewModel)
                        formName = "Авторизация";


                    if (form.ControlVisibility == Visibility.Visible)
                        _ = UpdateStatusText($"Открыта форма: {formName}");
                    else
                        _ = UpdateStatusText($"Закрыта форма: {formName}");
                }
            }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose() => LogVm?.Dispose();
    }

}


