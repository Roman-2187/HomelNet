using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class AdminMenuViewModel : FormViewModelBase
    {
        private readonly ILogQueueManager _adminLogQueueManager;

        public ILogQueueManager LogManager => _adminLogQueueManager;

        // Синхронизируем внутренний фильтр терминала
        [ObservableProperty] private LogLevelFilter _currentLogFilter = LogLevelFilter.All;

        // Динамические тексты кнопок — теперь они вычисляются на основе состояния навигатора!
        public string TableButtonText => Navigation.CurrentAdminTab == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";
        public string ToggleButtonText => Navigation.CurrentAdminTab == AdminSubTab.LogPanel ? "Скрыть лог" : "Показать лог";
        public string DeleteButtonText => Navigation.CurrentAdminTab == AdminSubTab.DeleteUserForm ? "Закрыть удаление" : "Удалить юзера";
        public string SeedButtonText => Navigation.CurrentAdminTab == AdminSubTab.SeedUsersForm ? "Закрыть сидинг" : "Сидинг Users";
        public string InspectorButtonText => Navigation.CurrentAdminTab == AdminSubTab.EventInspector ? "Закрыть граф" : "Инспектор шины";

        public AdminMenuViewModel(ILogQueueManager logQueueManager, IEventBus eventBus, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            _eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg =>
            {
                Navigation.SetAdminZone();
                NotifyAllButtonsChanged();
            });


            // 🎧 Ловим пинок отмены от формы удаления
            _eventBus.Subscribe<IAdminMenuViewModel.DeleteFormCloseRequested>(msg =>
            {
                // Схлопываем панель и сбрасываем тексты левых кнопок!
                ChangeTab(AdminSubTab.None);
            });
        }

        // Общая точка смены вкладок, которая оповещает XAML о том, что тексты кнопок изменились
        private void ChangeTab(AdminSubTab targetTab)
        {
            Navigation.ToggleAdminSubTab(targetTab);
            NotifyAllButtonsChanged();

            string statusText = Navigation.CurrentAdminTab == AdminSubTab.None
                ? "Панель управления очищена"
                : $"Переключение на panel: {Navigation.CurrentAdminTab}";
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(statusText));
        }

        private void NotifyAllButtonsChanged()
        {
            OnPropertyChanged(nameof(TableButtonText));
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(DeleteButtonText));
            OnPropertyChanged(nameof(SeedButtonText));
            OnPropertyChanged(nameof(InspectorButtonText));
        }

        [RelayCommand]
        private void ShowUserTable()
        {
            ChangeTab(AdminSubTab.UserTable);
            if (Navigation.CurrentAdminTab == AdminSubTab.UserTable)
            {
                _eventBus.Publish(this, new IAdminMenuViewModel.UserTableRequested());
            }
        }

        [RelayCommand]
        private void ShowLogPanel()
        {
            ChangeTab(AdminSubTab.LogPanel);
            if (Navigation.CurrentAdminTab == AdminSubTab.LogPanel)
            {
                _adminLogQueueManager.SetReady();
            }
        }

        [RelayCommand]
        private void ShowInspector()
        {
            ChangeTab(AdminSubTab.EventInspector);
            if (Navigation.CurrentAdminTab == AdminSubTab.EventInspector)
            {
                _eventBus.Publish(this, new IAdminMenuViewModel.ReportGenerationRequested());
            }
        }

        [RelayCommand]
        private void ShowDeleteForm()
        {
            ChangeTab(AdminSubTab.DeleteUserForm);

            // 🔥 Если админ открыл форму удаления — швыряем ивент в шину!
            if (Navigation.CurrentAdminTab == AdminSubTab.DeleteUserForm)
            {
                _eventBus.Publish(this, new IAdminMenuViewModel.DeleteFormRequested());
            }
        }


        [RelayCommand]
        private void ShowSeedForm() => ChangeTab(AdminSubTab.SeedUsersForm); // Рулёжка новым экраном!

        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter)
        {
            CurrentLogFilter = targetFilter;
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Лог-Фильтр]: Установлен режим {targetFilter}"));
        }
    }
}
