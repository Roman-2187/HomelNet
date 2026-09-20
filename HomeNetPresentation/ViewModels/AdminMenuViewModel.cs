using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class AdminMenuViewModel : FormViewModelBase
    {
        // 🔒 ИЗОЛИРОВАННЫЕ ПРИВАТНЫЕ ПОЛЯ
        private readonly IUserService _adminUserService;
        private readonly ILogQueueManager _adminLogQueueManager;
        

        // 🎛️ РУБИЛЬНИК 1: Локальная копия для реактивного изменения текста кнопок
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
        // Больше не нужно объявлять приватное поле _navigationStateManager! 
        // Конструктор просто пробрасывает навигатор в base:
        public AdminMenuViewModel(IUserService userService, ILogQueueManager logQueueManager, IEventBus eventBus, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _adminUserService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            _eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg =>
            {
                Navigation.SetAdminZone(); // 🎯 Используем свойство из базы!
                CurrentAdminTab = AdminSubTab.None;
            });
        }

        [RelayCommand]
        private void ShowUserTable()
        {
            Navigation.ToggleAdminSubTab(AdminSubTab.UserTable); // 🎯 Чисто и без дубликатов полей!
            CurrentAdminTab = Navigation.CurrentAdminTab;

            if (CurrentAdminTab == AdminSubTab.UserTable)
            {
                _eventBus.Publish(this, new IAdminMenuViewModel.UserTableRequested());
            }
        }


        // 📢 Кнопка: Показать/Скрыть панель логов
        [RelayCommand]
        private void ShowLogPanel()
        {
            Navigation.ToggleAdminSubTab(AdminSubTab.LogPanel);
            CurrentAdminTab = Navigation.CurrentAdminTab;

            if (CurrentAdminTab == AdminSubTab.LogPanel)
            {
                _adminLogQueueManager.SetReady();
            }
        }

        // 📢 Кнопка: Показать/Скрыть форму удаления
        [RelayCommand]
        private void ShowDeleteForm()
        {
            Navigation.ToggleAdminSubTab(AdminSubTab.DeleteUserForm);
            CurrentAdminTab = Navigation.CurrentAdminTab;
        }

        // 📢 Переключение внутренних микро-фильтров логов (Warning/Error/Critical)
        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter)
        {
            CurrentLogFilter = targetFilter;
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Лог-Фильтр]: Установлен режим {targetFilter}"));
        }

        // 📢 ВЗЛЁТ РАДАРА СОБЫТИЙ: Генерируем отчет из объектного графа Сервисов!
        [RelayCommand]
        private void ShowEventInspector()
        {
            Navigation.ToggleAdminSubTab(AdminSubTab.EventInspector);
            CurrentAdminTab = Navigation.CurrentAdminTab;

            if (CurrentAdminTab == AdminSubTab.EventInspector)
            {
                if (EventBus is IEventBus concreteBus)
                {
                    EventInspectorReport = concreteBus.GenerateInspectorReport();
                }
                else
                {
                    EventInspectorReport = "🧠 Ошибка: Не удалось подключиться к объектному графу шины событий.";
                }
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("Объектный граф шины событий успешно обновлен"));
            }
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
                        _eventBus.Publish(this, new IUsersTableViewModel.Added(user));
                        addedCount++;
                    }
                }

                string statusReport = addedCount > 0
                    ? $"Успешно добавлено {addedCount} тестовых юзеров."
                    : "Все пользователи уже добавлены!";

                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(statusReport));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка генерации: {ex.Message}"));
            }
        }

       

        #region 🧠 РЕАКТИВНЫЕ ПЕРЕХВАТЧИКИ ТЕКСТА КНОПОК

        // Следит за изменением локального макро-энума и автоматически меняет подписи кнопок
        partial void OnCurrentAdminTabChanged(AdminSubTab value)
        {
            ToggleButtonText = value == AdminSubTab.LogPanel ? "Скрыть лог" : "Показать лог";
            TableButtonText = value == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";

            // Отправляем чистый статус в строку состояния
            string statusText = value == AdminSubTab.None ? "Панель управления очищена" : $"Переключение на panel: {value}";
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(statusText));
        }

        #endregion
    }
}
