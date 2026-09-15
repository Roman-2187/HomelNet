using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Messaging;                  // Чистые сигналы-рекорды из Ядра
using HomeNetCore.Models;
using HomeNetPresentation.Enums;
using HomeNetServices.Services.Diagnostics;
using HomeNetServices.Services.Identity;
using HomeNetServices.Services.Messaging;               // Твой UserService из Сервисов

namespace HomeNetPresentation.ViewModels
{
   

    public partial class AdminMenuViewModel : FormViewModelBase
    {
        // 🔒 ИЗОЛИРОВАННЫЕ ПРИВАТНЫЕ ПОЛЯ (Компилятор больше не двоит!)
        private readonly UserService _adminUserService;
        private readonly LogQueueManager _adminLogQueueManager;

        // ВСЕГО ОДИН ХОЗЯИН ЭКРАНА! Заменяет кучу булевых флагов
        [ObservableProperty] private AdminSubPanelVisuability _activePanel = AdminSubPanelVisuability.None;

        // Живой текст для текстового радара событий
        [ObservableProperty] private string _eventInspectorReport = string.Empty;

        // Тексты кнопок-тумблеров
        [ObservableProperty] private string _toggleButtonText = "Показать лог";
        [ObservableProperty] private string _tableButtonText = "Показать users";

        // Конструктор — теперь принимает чистый IEventBus из Ядра! 🛸✨
        public AdminMenuViewModel(UserService userService, LogQueueManager logQueueManager, IEventBus eventBus) : base(eventBus)
        {
            _adminUserService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));
        }

        #region 🦾 ОДНОСТРОЧНЫЕ КОМАНДЫ ПЕРЕКЛЮЧЕНИЯ ПАНЕЛЕЙ (Toolkit)

        // 📢 Переключение на таблицу пользователей
        [RelayCommand]
        private  void ShowUserTable() => ActivePanel = AdminSubPanelVisuability.UserTable;

        // 📢 Переключение на форму удаления
        [RelayCommand]
        private void ShowDeleteForm() => ActivePanel = AdminSubPanelVisuability.DeleteUserForm;

        // 📢 Переключение на панель отладочных логов
        [RelayCommand]
        private void ShowLogPanel()
        {
            ActivePanel = AdminSubPanelVisuability.LogPanel;
            _adminLogQueueManager.SetReady(); // Пинаем конвейер задач логгера через переименованное поле
        }

        // 📢 ВЗЛЁТ РАДАРА СОБЫТИЙ: Генерируем отчет из объектного графа Сервисов! 🛸🎯
        // Чтобы Presentation не зависел от конкретного класса EventBus, мы вытаскиваем отчёт через каст к интерфейсу или кастомный метод
        [RelayCommand]
        private void ShowEventInspector()
        {
            ActivePanel = AdminSubPanelVisuability.EventInspector;

            // Дёргаем инспектора напрямую через наш зашитый в базу EventBus
            if (EventBus is EventBus concreteBus)
            {
                EventInspectorReport = concreteBus.Inspector.GenerateReport();
            }
            else
            {
                EventInspectorReport = "🧠 Ошибка: Не удалось подключиться к объектному графу шины событий.";
            }

            _eventBus.Publish(this, new StatusTextChangedMessage("Объектный граф шины событий успешно обновлен"));
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
                        await _adminUserService.AddUserAsync(user);
                        _eventBus.Publish(this, new UserAddedMessage(user));
                        addedCount++;
                    }
                }

                string statusReport = addedCount > 0
                    ? $"Успешно добавлено {addedCount} тестовых юзеров."
                    : "Все пользователи уже добавлены!";

                _eventBus.Publish(this, new StatusTextChangedMessage(statusReport));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new StatusTextChangedMessage($"Ошибка генерации: {ex.Message}"));
            }
        }

        #endregion

        #region 🧠 РЕАКТИВНЫЕ ПЕРЕХВАТЧИКИ ТЕКСТА КНОПОК

        // Следит за изменением стейта и автоматически меняет подписи кнопок
        partial void OnActivePanelChanged(AdminSubPanelVisuability value)
        {
            ToggleButtonText = value == AdminSubPanelVisuability.LogPanel ? "Скрыть лог" : "Показать лог";
            TableButtonText = value == AdminSubPanelVisuability.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";

            // Отправляем чистый статус в строку состояния
            string statusText = value == AdminSubPanelVisuability.None ? "Панель управления очищена" : $"Переключение на панель: {value}";
            _eventBus.Publish(this, new StatusTextChangedMessage(statusText));
        }

        #endregion
    }
}
