using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetPresentation.Services
{
    /// <summary>
    /// Автономный менеджер навигации зоны администратора SiberNet.
    /// Переключает макро-зону и возвращает полный дуэт стейтов в тайтлбар.
    /// </summary>
    public partial class AdminNavigationManager : NavigationStateManager
    {
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;

        public AdminNavigationManager(IEventBus eventBus) : base(eventBus)
        {
            EventBus.Subscribe<IAdminVm.RequestAdminZone>(OnRequestAdminZone);
            EventBus.Subscribe<IAdminVm.LogoutAdmin>(OnLogoutAdminRequested);

            EventBus.Subscribe<IAdminVm.OpenTableUsers>(OnOpenTableUsers);
            EventBus.Subscribe<IAdminVm.OpenSeedUsers>(OnOpenSeedUsers);
            EventBus.Subscribe<IAdminVm.OpenDelete>(OnOpenDelete);
            EventBus.Subscribe<IAdminVm.OpenLogger>(OnOpenLogger);
            EventBus.Subscribe<IAdminVm.OpenEventInspector>(OnOpenEventInspector);
        }

        #region 👑 МАКРО-УПРАВЛЕНИЕ ЗОНОЙ АДМИНА

        private void OnRequestAdminZone(IAdminVm.RequestAdminZone msg)
        {
            CurrentMainZone = MainTab.AdminZone;
            

            // 🔥 ПИНГ-ПОНГ: Передаем макро-зону Админа + тушим клиентский таб в None!
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, ClientSubTab.None));

            // Локальный факт для вложенного холста админки
            EventBus.Publish(this, new IAdminVm.AdminTabChanged(CurrentAdminTab));
        }

        private void OnLogoutAdminRequested(IAdminVm.LogoutAdmin msg)
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.StartZone;
            CurrentAdminTab = AdminSubTab.None; // Выключили свет за собой

            // 🔥 ПИНГ-ПОНГ: Возвращаем полную макро-заставку (StartZone + None)
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, ClientSubTab.None));
            EventBus.Publish(this, new IAdminVm.AdminTabChanged(CurrentAdminTab));
        }

        #endregion

        #region 🎧 ЛОКАЛЬНЫЕ ВНУТРЕННИЕ ВКЛАДКИ АДМИНКИ

        private void OnOpenTableUsers(IAdminVm.OpenTableUsers msg) => SwitchAdminTab(AdminSubTab.UserTable);
        private void OnOpenSeedUsers(IAdminVm.OpenSeedUsers msg) => SwitchAdminTab(AdminSubTab.SeedUsers);
        private void OnOpenDelete(IAdminVm.OpenDelete msg) => SwitchAdminTab(AdminSubTab.DeleteUsers);
        private void OnOpenLogger(IAdminVm.OpenLogger msg) => SwitchAdminTab(AdminSubTab.Logger);
        private void OnOpenEventInspector(IAdminVm.OpenEventInspector msg) => SwitchAdminTab(AdminSubTab.EventInspector);

        #endregion

        private void SwitchAdminTab(AdminSubTab targetTab)
        {
            if (CurrentMainZone != MainTab.AdminZone)
            {
                CurrentMainZone = MainTab.AdminZone;
                EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, ClientSubTab.None));
            }

            CurrentAdminTab = CurrentAdminTab == targetTab ? AdminSubTab.None : targetTab;
            EventBus.Publish(this, new IAdminVm.AdminTabChanged(CurrentAdminTab));
        }

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<IAdminVm.RequestAdminZone>(OnRequestAdminZone);
            EventBus.Unsubscribe<IAdminVm.LogoutAdmin>(OnLogoutAdminRequested);
            EventBus.Unsubscribe<IAdminVm.OpenTableUsers>(OnOpenTableUsers);
            EventBus.Unsubscribe<IAdminVm.OpenSeedUsers>(OnOpenSeedUsers);
            EventBus.Unsubscribe<IAdminVm.OpenDelete>(OnOpenDelete);
            EventBus.Unsubscribe<IAdminVm.OpenLogger>(OnOpenLogger);
            EventBus.Unsubscribe<IAdminVm.OpenEventInspector>(OnOpenEventInspector);
        }
    }
}
