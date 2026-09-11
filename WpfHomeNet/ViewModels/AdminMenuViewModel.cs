using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers;

namespace WpfHomeNet.ViewModels
{
    public partial class AdminMenuViewModel : FormViewModelBase
    {
        #region Поля 🦾
        private readonly UserService _userService;
        private readonly LogQueueManager _logQueueManager; // 🔥 Добавили менеджер сюда

        [ObservableProperty]
        private string _toggleButtonText = "Показать лог";

        [ObservableProperty]
        private string _tableButtonText = "Показать users";

        // 🔥 Сделали свойство видимости лога автоматическим и наблюдаемым!
        [ObservableProperty]
        private bool _isLogVisible = false;

        [ObservableProperty]
        private bool _isTableVisible = false;
        #endregion

        #region Команды
        public ICommand ToggleLogWindowCommand { get; private set; } = null!;
        public ICommand UserTableViewCommand { get; private set; } = null!;
        public ICommand SeedDataCommand { get; private set; } = null!;
        #endregion

        #region Конструктор
        public AdminMenuViewModel(UserService userService, LogQueueManager logQueueManager, EventBus eventBus) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            InitializeCommands();
            InitializeBusSubscriptions();
        }
        #endregion

        #region Инициализация команд и подписок
        private void InitializeCommands()
        {
            // 🎯 ТУТ ВСЯ МАГИЯ: Клик по кнопке теперь рулит и свойством, и запуском бэкенда логов!
            ToggleLogWindowCommand = new RelayCommand(_ =>
            {
                // Инвертируем видимость
                IsLogVisible = !IsLogVisible;

                // Переключаем текст на кнопке
                ToggleButtonText = IsLogVisible ? "Скрыть лог" : "Показать лог";

                if (IsLogVisible)
                {
                    // 🚀 ВКЛЮЧАЕМ: Если админ открыл панель логов — пинаем конвейер задач
                    _logQueueManager.SetReady();
                }

                // Старая подписка через шину (если нужно для внешних окон)
                _eventBus.Publish(new LogWindowVisibilityChangedMessage(IsLogVisible));
            });

            UserTableViewCommand = new RelayCommand(_ => ToggleUserTable());
            SeedDataCommand = new RelayCommand(async _ => await ExecuteSeedDataAsync());
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<LogWindowVisibilityChangedMessage>(msg =>
            {
                IsLogVisible = msg.IsVisible;
                ToggleButtonText = IsLogVisible ? "Скрыть лог" : "Показать лог";
            });
        }
        #endregion

        #region Логика тумблера таблицы
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

