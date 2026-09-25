using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using System;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 Добавили реализацию IDisposable для безопасной отписки
    public partial class AdminMenuViewModel : FormViewModelBase, IDisposable
    {
        private readonly ILogQueueManager _adminLogQueueManager;

        public ILogQueueManager LogManager => _adminLogQueueManager;

        [ObservableProperty] private LogLevelFilter _currentLogFilter = LogLevelFilter.All;

        public string TableButtonText => Navigation.CurrentAdminTab == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";
        public string ToggleButtonText => Navigation.CurrentAdminTab == AdminSubTab.LogPanel ? "Скрыть лог" : "Показать лог";
        public string DeleteButtonText => Navigation.CurrentAdminTab == AdminSubTab.DeleteUserForm ? "Закрыть удаление" : "Удалить юзера";
        public string SeedButtonText => Navigation.CurrentAdminTab == AdminSubTab.SeedUsersForm ? "Закрыть сидинг" : "Сидинг Users";
        public string InspectorButtonText => Navigation.CurrentAdminTab == AdminSubTab.EventInspector ? "Закрыть граф" : "Инспектор шины";

        public AdminMenuViewModel(ILogQueueManager logQueueManager, IEventBus eventBus, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            // 🔥 ЧИСТОТА: Передали имена методов вместо старых анонимных стрелочных функций!
            _eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(OnUserLogged);
            _eventBus.Subscribe<IAdminMenuViewModel.DeleteFormCloseRequested>(OnDeleteFormCloseRequested);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для отображения в Инспекторе) 🧼

        private void OnUserLogged(IAuthenticationViewModel.UserLogged msg)
        {
            Navigation.SetAdminZone();
            NotifyAllButtonsChanged();
        }

        private void OnDeleteFormCloseRequested(IAdminMenuViewModel.DeleteFormCloseRequested msg)
        {
            ChangeTab(AdminSubTab.None);
        }

        #endregion

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

            if (Navigation.CurrentAdminTab == AdminSubTab.DeleteUserForm)
            {
                _eventBus.Publish(this, new IAdminMenuViewModel.DeleteFormRequested());
            }
        }

        [RelayCommand]
        private void ShowSeedForm() => ChangeTab(AdminSubTab.SeedUsersForm);

        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter)
        {
            CurrentLogFilter = targetFilter;
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Лог-Фильтр]: Установлен режим {targetFilter}"));
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Вызывается системой при закрытии компонента. Полностью вырезает ссылки на методы из шины событий.
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe<IAuthenticationViewModel.UserLogged>(OnUserLogged);
            _eventBus.Unsubscribe<IAdminMenuViewModel.DeleteFormCloseRequested>(OnDeleteFormCloseRequested);
        }

        #endregion
    }
}