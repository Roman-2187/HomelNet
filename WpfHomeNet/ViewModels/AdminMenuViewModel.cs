using HomeNetCore.Models;
using HomeNetCore.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    
    public class AdminMenuViewModel : INotifyPropertyChanged
    {
        #region Поля и переменные
        private readonly UserService _userService;
        private readonly EventBus _eventBus;

        private string _toggleButtonText = "Показать лог";
        private string _tableButtonText = "Показать users";
        private bool _isTableVisible;
        private bool _isLogVisible;
        #endregion

        #region Свойства и Команды
        public ICommand ToggleLogWindowCommand { get; }
        public ICommand UserTableViewCommand { get; }
        public ICommand SeedDataCommand { get; }

        public string ToggleButtonText
        {
            get => _toggleButtonText;
            set => SetField(ref _toggleButtonText, value);
        }

        public string TableButtonText
        {
            get => _tableButtonText;
            set => SetField(ref _tableButtonText, value);
        }
        #endregion

        #region Конструктор
        public AdminMenuViewModel(UserService userService, EventBus eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            ToggleLogWindowCommand = new RelayCommand(_ =>
            {
                // Оставляем ТОЛЬКО публикацию в автобус. Старый вызов делегата удаляем!
                _eventBus.Publish(new LogWindowVisibilityChangedMessage(!_isLogVisible));
            });

            UserTableViewCommand = new RelayCommand(_ =>
                _eventBus.Publish(new UserTableVisibilityChangedMessage(!_isTableVisible)));

            SeedDataCommand = new RelayCommand(async _ => await ExecuteSeedDataAsync());

            // Подписываемся на события изменения видимости окон из воздуха
            _eventBus.Subscribe<LogWindowVisibilityChangedMessage>(msg =>
            {
                _isLogVisible = msg.IsVisible;
                ToggleButtonText = _isLogVisible ? "Скрыть лог" : "Показать лог";
            });

            _eventBus.Subscribe<UserTableVisibilityChangedMessage>(msg =>
            {
                _isTableVisible = msg.IsVisible;
                TableButtonText = _isTableVisible ? "Скрыть users" : "Показать users";
            });
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}

