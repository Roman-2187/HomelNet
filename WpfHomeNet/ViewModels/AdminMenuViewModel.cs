
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : FormViewModelBase
    {
        #region Поля и переменные
        private readonly UserService _userService;
        private string _toggleButtonText = "Показать лог";
        private string _tableButtonText = "Показать users";
        private bool _isLogVisible;
        private bool _isTableVisible = false; // Изначально скрыта
        #endregion

        #region Свойства
        // Чистое MVVM свойство! Его изменение мгновенно взрывает XAML обновлением экрана! 🧼
        public bool IsTableVisible
        {
            get => _isTableVisible;
            set => SetField(ref _isTableVisible, value);
        }

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

        // Команды
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
            // Логи пускай вещаются наружу через автобус, раз окно логов внешнее
            ToggleLogWindowCommand = new RelayCommand(_ =>
            {
                _eventBus.Publish(new LogWindowVisibilityChangedMessage(!_isLogVisible));
            });

            // ИСПРАВЛЕНИЕ: Кнопка жестко, напрямую вызывает наш метод-тумблер! Без посредников! 🦾
            UserTableViewCommand = new RelayCommand(_ => ToggleUserTable());

            SeedDataCommand = new RelayCommand(async _ => await ExecuteSeedDataAsync());
        }

        private void InitializeBusSubscriptions()
        {
            // Слушаем только лог-окно
            _eventBus.Subscribe<LogWindowVisibilityChangedMessage>(msg =>
            {
                _isLogVisible = msg.IsVisible;
                ToggleButtonText = _isLogVisible ? "Скрыть лог" : "Показать лог";
            });

            // СТРАННАЯ ПОДПИСКА НА САМОГО СЕБЯ УДАЛЕНА НАХРЕН! 🧹
        }
        #endregion

        #region Логика тумблера таблицы (Прямая и неуязвимая)
        private void ToggleUserTable()
        {
            // 1. Прямая инверсия свойства! Сеттер сам вызовет SetField и пнёт WPF! 🔔
            IsTableVisible = !IsTableVisible;

            // 2. Переключаем текст на кнопке на лету
            TableButtonText = IsTableVisible ? "Скрыть users 🙈" : "Показать users 👁️";

            // Сигнал в шину для статус-бара
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
