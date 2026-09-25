using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    // 🔥 Реализуем IDisposable для абсолютной потоковой безопасности и зачистки памяти
    public partial class NavigationStateManager : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;

        [ObservableProperty] private MainTab _currentMainZone = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;
        [ObservableProperty] private UserEntity? _currentUser;

        public NavigationStateManager(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 🔥 ЧИСТОТА: Заменили стрелочные лямбды на ссылки на именованные методы класса! 🧼
            _eventBus.Subscribe<ITitleBarViewModel.MacroNavigation>(OnMacroNavigationRequested);
            _eventBus.Subscribe<ITitleBarViewModel.ClientTabChanged>(OnClientTabChangedRequested);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnMacroNavigationRequested(ITitleBarViewModel.MacroNavigation msg)
        {
            if (msg.TargetTab == MainTab.None)
            {
                SetStartZone();
            }
            else if (msg.TargetTab == MainTab.AdminZone)
            {
                SetAdminZone();
            }
        }

        private void OnClientTabChangedRequested(ITitleBarViewModel.ClientTabChanged msg)
        {
            SetAuthZone(msg.TargetSubTab);
        }

        #endregion

        // Включаем режим: Стартовая страница (Абсолютный ноль)
        public void SetStartZone()
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = ClientSubTab.None;
            CurrentAdminTab = AdminSubTab.None;

            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Форма авторизации (Вход / Регистрация)
        public void SetAuthZone(ClientSubTab formTab)
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = formTab;
            CurrentAdminTab = AdminSubTab.None;

            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Клиент зашел в чат
        public void SetClientZone(UserEntity user)
        {
            CurrentUser = user;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            CurrentAdminTab = AdminSubTab.None;

            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Админ панель (Главный экран, вкладки очищены)
        public void SetAdminZone()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.None;
            CurrentClientTab = ClientSubTab.None;

            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Внутренний тумблер随Админки: Включение под-экранов (Таблица, Логи, Удаление)
        public void ToggleAdminSubTab(AdminSubTab targetTab)
        {
            if (CurrentMainZone != MainTab.AdminZone) return;

            CurrentAdminTab = CurrentAdminTab == targetTab ? AdminSubTab.None : targetTab;
        }

        // Полный сброс при выходе
        public void ClearToGuest()
        {
            CurrentUser = null;
            SetStartZone();
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Полностью вырезает ссылки на методы навигатора из глобальной шины событий.
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe<ITitleBarViewModel.MacroNavigation>(OnMacroNavigationRequested);
            _eventBus.Unsubscribe<ITitleBarViewModel.ClientTabChanged>(OnClientTabChangedRequested);
        }

        #endregion
    }
}