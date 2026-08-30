using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services.ListUsersServise;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel; 
using CommunityToolkit.Mvvm.Input;        
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{   
    public partial class MainViewModel : FormViewModelBase
    {
        #region Поля (Автоматический штамповочный цех Microsoft) 🦾
        private readonly ListUsersService _listUsersService;
        private readonly ILogger _logger;

        [ObservableProperty]
        private Visibility _mainInterfaceVisibility = Visibility.Collapsed; 

        [ObservableProperty]
        private Visibility _adminMenuVisibility = Visibility.Collapsed; 
        #endregion

        #region Свойства зависимостей (Дочерние формы и коллекции)
        public RegistrationViewModel? RegistrationViewModel { get; set; } //
        public DeletionUsersViewModel? DeleteUsersViewModel { get; set; } //
        public AuthenticationViewModel? LoginViewModel { get; set; } //
        public AdminMenuViewModel? AdminMenuViewModel { get; set; } //
        public LogWindow? LogWindow { get; set; } //
        public LogViewModel? LogVm { get; set; } //
        public StatusBarViewModel? StatusBarViewModel { get; set; } //

        public EventBus EventBus => _eventBus; //
        public ObservableCollection<UserEntity> Users => _listUsersService.Users; //

        // Вычисляемый реактивный датчик панели кнопок
        public bool IsButtonsPanelEnabled =>
            !(RegistrationViewModel?.ControlVisibility == Visibility.Visible ||
            LoginViewModel?.ControlVisibility == Visibility.Visible ||
            DeleteUsersViewModel?.ControlVisibility == Visibility.Visible); //
        #endregion

        #region Конструктор
        public MainViewModel(ILogger logger, EventBus eventBus, ListUsersService listUsersService) : base(eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); //
            _listUsersService = listUsersService ?? throw new ArgumentNullException(nameof(listUsersService)); //

            InitializeBusSubscriptions(); //
        }
        #endregion

        #region Инициализация подписок шины (Только работа с коллекцией)
        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId); //
                if (userToRemove != null)
                {
                    Application.Current.Dispatcher.Invoke(() => Users.Remove(userToRemove)); //
                }
            });

            _eventBus.Subscribe<FormVisibilityChangedMessage>(msg =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Сигнализируем интерфейсу пересчитать статус кнопочной панели
                    OnPropertyChanged(nameof(IsButtonsPanelEnabled));
                });
            });

            _eventBus.Subscribe<AdminMenuVisibilityChangedMessage>(msg =>
            {
                // Пишем строго в публичное свойство с Большой буквы! 🧼
                AdminMenuVisibility = msg.IsVisible ? Visibility.Visible : Visibility.Collapsed; //
            });

            _eventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User == null) return; //
                Application.Current.Dispatcher.Invoke(() => Users.Add(msg.User)); //
            });
        }
        #endregion

        #region Логика работы
        public async Task InitializeAsync()
        {
            try
            {
                _eventBus.Publish(new StatusTextChangedMessage("Синхронизация с базой данных HomeNet...")); //
                await _listUsersService.RefreshUsersAsync(); //
                _eventBus.Publish(new UsersListRefreshedMessage(Users)); //
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при старте приложения: {ex.Message}"); //
                _eventBus.Publish(new StatusTextChangedMessage("Ошибка подключения к СУБД ❌")); //
            }
        }
        #endregion

        #region НАНО-КОМАНДЫ (Студия сама проштампует свойства со словом Command на конце!) 🛸

        // 1. Сгенерирует свойство: ToggleAdminMenuCommand
        [RelayCommand]
        private void ToggleAdminMenu()
        {
            bool isCurrentlyVisible = AdminMenuVisibility == Visibility.Visible; //
            _eventBus.Publish(new AdminMenuVisibilityChangedMessage(!isCurrentlyVisible)); //
        }

        // 2. Сгенерирует свойство: LogoutCommand
        [RelayCommand]
        private void Logout()
        {
            MainInterfaceVisibility = Visibility.Collapsed; //
            OnGlobalResetRequested?.Invoke(); //
            _eventBus.Publish(new StatusTextChangedMessage("Выход из аккаунта выполнен успешно")); //
        }

       
        [RelayCommand]
        private void ToggleFormVisibility(object parameter)
        {
            if (parameter is FormViewModelBase vm) 
            {
                if (MainInterfaceVisibility == Visibility.Visible) return; 

                vm.ControlVisibility = vm.ControlVisibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; 
                OnPropertyChanged(nameof(IsButtonsPanelEnabled)); 
                _eventBus.Publish(new FormVisibilityChangedMessage(vm.GetType(), vm.ControlVisibility)); 
            }
        }

        #endregion
    }
}
