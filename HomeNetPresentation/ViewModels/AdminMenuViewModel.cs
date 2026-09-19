using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging.HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetPresentation.ViewModels
{
    public partial class AdminMenuViewModel : FormViewModelBase
    {
        // 🔒 ИЗОЛИРОВАННЫЕ ПРИВАТНЫЕ ПОЛЯ
        private readonly IUserService _adminUserService;
        private readonly ILogQueueManager _adminLogQueueManager;

        // 🎛️ РУБИЛЬНИК 1: Главный макро-экран админки
        [ObservableProperty]
        private AdminSubTab _currentAdminTab = AdminSubTab.None;

        // 🎛️ РУБИЛЬНИК 2: Микро-фильтр внутри панели логов
        [ObservableProperty]
        private LogLevelFilter _currentLogFilter = LogLevelFilter.All;

        // Живой текст для текстового радара событий
        [ObservableProperty] private string _eventInspectorReport = string.Empty;

        // Тексты кнопок-тумблеров
        [ObservableProperty] private string _toggleButtonText = "Показать лог";
        [ObservableProperty] private string _tableButtonText = "Показать users";

        // Конструктор — принимает чистый IEventBus из Ядра
        public AdminMenuViewModel(IUserService userService, ILogQueueManager logQueueManager, IEventBus eventBus) : base(eventBus)
        {
            _adminUserService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));
        }

        #region 🦾 ОДНОСТРОЧНЫЕ КОМАНДЫ ПЕРЕКЛЮЧЕНИЯ ПАНЕЛЕЙ (Toolkit)

        // 📢 Переключение на таблицу пользователей
        [RelayCommand]
        private void ShowUserTable() => CurrentAdminTab = AdminSubTab.UserTable;

        // 📢 Переключение на форму удаления
        [RelayCommand]
        private void ShowDeleteForm() => CurrentAdminTab = AdminSubTab.DeleteUserForm;

        // 📢 Переключение на панель отладочных логов
        [RelayCommand]
        private void ShowLogPanel()
        {
            CurrentAdminTab = AdminSubTab.LogPanel;
            _adminLogQueueManager.SetReady();
        }

        // 📢 Переключение внутренних микро-фильтров логов (Warning/Error/Critical)
        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter)
        {
            CurrentLogFilter = targetFilter;

            // 🔥 ПОПРАВИЛИ: Сигнал в строку состояния через новый короткий рекорд хозяина
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Лог-Фильтр]: Установлен режим {targetFilter}"));
        }

        // 📢 ВЗЛЁТ РАДАРА СОБЫТИЙ: Генерируем отчет из объектного графа Сервисов!
        [RelayCommand]
        private void ShowEventInspector()
        {
            CurrentAdminTab = AdminSubTab.EventInspector;

            if (EventBus is IEventBus concreteBus)
            {
                // Подтягиваем сгенерированный отчет из нашего EventBusInspector
                EventInspectorReport = concreteBus.GenerateInspectorReport();
            }
            else
            {
                EventInspectorReport = "🧠 Ошибка: Не удалось подключиться к объектному графу шины событий.";
            }

            // 🔥 ПОПРАВИЛИ: Короткий рекорд
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Объектный граф шины событий успешно обновлен"));
        }

        // 📢 СИДИНГ ТЕСТОВЫХ ДАННЫХ
        [RelayCommand]
        private async Task SeedDataAsync()
        {
            int addedCount = 0;
            try
            {
                var testUsers = DbSeedData.GetGeneratedUsers();

                foreach (var user in testUsers)
                {
                    bool emailExists = await _adminUserService.CheckEmailExistsAsync(user.Email);
                    if (!emailExists)
                    {
                        await _adminUserService.AddUserSecureAsync(user);

                        // 🔥 ПОПРАВИЛИ: Сигнал добавления юзера улетел через короткий рекорд таблицы
                        _eventBus.Publish(this, new IUsersTableViewModel.Added(user));
                        addedCount++;
                    }
                }

                string statusReport = addedCount > 0
                    ? $"Успешно добавлено {addedCount} тестовых юзеров."
                    : "Все пользователи уже добавлены!";

                // 🔥 ПОПРАВИЛИ: Короткий рекорд
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(statusReport));
            }
            catch (Exception ex)
            {
                // 🔥 ПОПРАВИЛИ: Короткий рекорд
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка генерации: {ex.Message}"));
            }
        }

        #endregion

        #region 🧠 РЕАКТИВНЫЕ ПЕРЕХВАТЧИКИ ТЕКСТА КНОПОК

        // Следит за изменением главного макро-энума и автоматически меняет подписи кнопок
        partial void OnCurrentAdminTabChanged(AdminSubTab value)
        {
            ToggleButtonText = value == AdminSubTab.LogPanel ? "Скрыть лог" : "Показать лог";
            TableButtonText = value == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";

            // Отправляем чистый статус в строку состояния
            string statusText = value == AdminSubTab.None ? "Панель управления очищена" : $"Переключение на panel: {value}";

            // 🔥 ПОПРАВИЛИ: Короткий рекорд
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(statusText));
        }

        #endregion
    }
}
