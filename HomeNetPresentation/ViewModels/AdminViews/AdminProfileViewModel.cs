using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class AdminProfileViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly ILogQueueManager _adminLogQueueManager;

        // Локальный стейт вкладки, скопированный из факта навигатора
        

        [ObservableProperty] private LogLevelFilter _currentLogFilter = LogLevelFilter.All;


        [ObservableProperty] private AdminSubTab _activeTab = AdminSubTab.None;


        // Тексты кнопок читают наше локальное свойство
        public string TableButtonText => ActiveTab == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";
        public string ToggleButtonText => ActiveTab == AdminSubTab.Logger ? "Скрыть лог" : "Показать лог";
        public string DeleteButtonText => ActiveTab == AdminSubTab.DeleteUsers ? "Закрыть удаление" : "Удалить юзера";
        public string SeedButtonText => ActiveTab == AdminSubTab.SeedUsers ? "Закрыть сидинг" : "Сидинг Users";
        public string InspectorButtonText => ActiveTab == AdminSubTab.EventInspector ? "Закрыть граф" : "Инспектор шины";

        public AdminProfileViewModel(
            ILogQueueManager logQueueManager,
            IEventBus eventBus,
            AdminNavigationManager navigation) : base(eventBus, navigation)
        {
            _adminLogQueueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            // 🔥 ЧИСТОТА ДЛЯ ИНСПЕКТОРА: Ловим факт изменения вкладки именованным методом!
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Ловим пули от Навигатора) 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // 🔥 Пишем в генерируемое Toolkit-ом свойство (с большой буквы)
            ActiveTab = msg.ActiveTab;

            if (ActiveTab == AdminSubTab.Logger)
            {
                _adminLogQueueManager.SetReady();
            }

            NotifyAllButtonsChanged();
        }

        #endregion

        private void NotifyAllButtonsChanged()
        {
            OnPropertyChanged(nameof(TableButtonText));
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(DeleteButtonText));
            OnPropertyChanged(nameof(SeedButtonText));
            OnPropertyChanged(nameof(InspectorButtonText));
        }

        #region 🚀 МАРШРУТИЗАЦИЯ НАМЕРЕНИЙ (Публикация в автобус) 🧼

        [RelayCommand] public void ShowUserTable() => _eventBus.Publish(this, new IAdminVm.OpenTableUsers());
        [RelayCommand] private void ShowLogPanel() => _eventBus.Publish(this, new IAdminVm.OpenLogger());
        [RelayCommand] private void ShowInspector() => _eventBus.Publish(this, new IAdminVm.OpenEventInspector());
        [RelayCommand] private void ShowDeleteForm() => _eventBus.Publish(this, new IAdminVm.OpenDelete());
        [RelayCommand] private void ShowSeedForm() => _eventBus.Publish(this, new IAdminVm.OpenSeedUsers());
        [RelayCommand] private void Logout() => _eventBus.Publish(this, new IAdminVm.LogoutAdmin());

        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter)
        {
            CurrentLogFilter = targetFilter;
            _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Лог-Фильтр]: Установлен режим {targetFilter}"));
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }
    }
}
