
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel; 
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class AdminMenuViewModel : FormViewModelBase
    {
        #region Поля (Штамповочный цех генератора) 🦾
        private readonly UserService _userService;

        [ObservableProperty]
        private string _toggleButtonText = "Показать лог"; 

        [ObservableProperty]
        private string _tableButtonText = "Показать users"; 

        private bool _isLogVisible;

        [ObservableProperty]
        private bool _isTableVisible = false; 
        #endregion

        #region Команды
        public ICommand ToggleLogWindowCommand { get; private set; } = null!;
        public ICommand UserTableViewCommand { get; private set; } = null!;
        public ICommand SeedDataCommand { get; private set; } = null!;
        #endregion

        #region Конструктор
        public AdminMenuViewModel(UserService userService, EventBus eventBus) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeCommands();
            InitializeBusSubscriptions();
        }
        #endregion

        #region Инициализация команд и подписок
        private void InitializeCommands()
        {
            ToggleLogWindowCommand = new RelayCommand(_ =>
            {
                _eventBus.Publish(new LogWindowVisibilityChangedMessage(!_isLogVisible));
            });
     
            UserTableViewCommand = new RelayCommand(_ => ToggleUserTable());

            SeedDataCommand = new RelayCommand(async _ => await ExecuteSeedDataAsync());
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<LogWindowVisibilityChangedMessage>(msg =>
            {
                _isLogVisible = msg.IsVisible;
                ToggleButtonText = _isLogVisible ? "Скрыть лог" : "Показать лог"; // Работаем через Большую букву!
            });
        }
        #endregion

        #region Логика тумблера таблицы (Прямая и неуязвимая)
        private void ToggleUserTable()
        {      
            IsTableVisible = !IsTableVisible;
        
            TableButtonText = IsTableVisible ? "Скрыть users 🙈" : "Показать users 👁️";
            
            string status = IsTableVisible ? "Таблица пользователей открыта" : "Таблица пользователей скрыта";
            _eventBus.Publish(new StatusTextChangedMessage(status));
        }
        #endregion

        #region Логика сидинга данных
        private async Task ExecuteSeedDataAsync()
        {
            int addedCount = 0;
            try
            {
                foreach (var user in DbSeedData.Users)
                {
                    bool emailExists = await _userService.CheckEmailExistsAsync(user.Email);
                    if (!emailExists)
                    {
                        await _userService.AddUserAsync(user);
                        _eventBus.Publish(new UserAddedMessage(user));
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно добавлено {addedCount} тестовых юзеров.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Все пользователи уже добавлены!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации: {ex.Message}");
            }
        }
        #endregion
    }
}
