using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    public partial class NavigationStateManager : ObservableObject
    {
        // Наш единственный источник правды для XAML DataTrigger-ов
        [ObservableProperty] private MainTab _currentMainZone = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;
        [ObservableProperty] private UserEntity? _currentUser;

        // Включаем режим: Стартовая страница (Абсолютный ноль)
        public void SetStartZone()
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = ClientSubTab.None;
            CurrentAdminTab = AdminSubTab.None;
        }

        // Включаем режим: Форма авторизации (Вход / Регистрация)
        public void SetAuthZone(ClientSubTab formTab)
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = formTab;
            CurrentAdminTab = AdminSubTab.None;
        }

        // Включаем режим: Клиент зашел в чат
        public void SetClientZone(UserEntity user)
        {
            CurrentUser = user;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            CurrentAdminTab = AdminSubTab.None;
        }

        // Включаем режим: Админ панель (Главный экран, вкладки очищены)
        public void SetAdminZone()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.None;
            CurrentClientTab = ClientSubTab.None;
        }

        // Внутренний тумблер Админки: Включение под-экранов (Таблица, Логи, Удаление)
        // Если кликнули на ту же самую вкладку — она закроется в None
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

        // Умный геттер для видимости кнопки возврата (остается без изменений)
        public bool IsAdminReturnButtonVisible =>
            CurrentUser != null &&
            CurrentMainZone == MainTab.ClientZone &&
            CurrentClientTab == ClientSubTab.Messenger &&
            (CurrentUser.Email?.Contains("admin", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
