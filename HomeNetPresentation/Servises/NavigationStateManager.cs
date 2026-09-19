using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // 🔥 Где лежат все наши новые чистые интерфейсы рекордов
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    public partial class NavigationStateManager : ObservableObject
    {
        [ObservableProperty] private MainTab _currentMainZone = MainTab.ClientZone;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None; // Стартовый хаб
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;   // Стерильный ноль!
        [ObservableProperty] private UserEntity? _currentUser;

        public NavigationStateManager(IEventBus eventBus)
        {
            // 🛡️ 1. УСПЕШНЫЙ ВХОД
            // 🔥 ПОПРАВИЛИ: Слушаем короткий рекорд из интерфейса входа
            eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg =>
            {
                CurrentUser = msg.User;

                CurrentMainZone = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger; // СРАЗУ открываем мессенджер (со списком юзеров!)
                CurrentAdminTab = AdminSubTab.None;        // Админка спит
            });

            // 🧬 2. УСПЕШНАЯ РЕГИСТРАЦИЯ
            // 🔥 ПОПРАВИЛИ: Слушаем короткий рекорд из интерфейса таблицы пользователей
            eventBus.Subscribe<IUsersTableViewModel.Added>(msg =>
            {
                CurrentUser = msg.User;

                CurrentMainZone = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger; // Тоже сразу отправляем в чаты
                CurrentAdminTab = AdminSubTab.None;
            });
        }

        // ====== 🦾 РУЧНОЕ УПРАВЛЕНИЕ ======

        // Ручной переход в режим админки
        [RelayCommand]
        public void OpenAdminZone()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.None;   // 🔥 ЖЕСТКО: Ничего не стартует сразу! Чистый None
            CurrentClientTab = ClientSubTab.None; // Гасим клиентский хаб
        }

        // Ручной возврат из админки обратно в чат
        [RelayCommand]
        public void ReturnToMessenger()
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            CurrentAdminTab = AdminSubTab.None;
        }

        [RelayCommand]
        public void NavigateInAdmin(AdminSubTab subTab)
        {
            if (CurrentMainZone != MainTab.AdminZone) return;
            CurrentAdminTab = subTab; // Админ сам манипулирует своими экранами по кнопкам
        }

        [RelayCommand]
        public void NavigateInClient(ClientSubTab subTab)
        {
            CurrentClientTab = subTab;
        }

        // 🧼 ТОТАЛЬНЫЙ СБРОС (Выйти из аккаунта)
        [RelayCommand]
        public void ResetToGuest()
        {
            CurrentUser = null;
            CurrentAdminTab = AdminSubTab.None;
            CurrentClientTab = ClientSubTab.None; // Возврат на Welcome-экран
            CurrentMainZone = MainTab.ClientZone;
        }

        // 🛠️ КНОПКА В АДМИНКЕ: "Открыть Мессенджер" (Прямой прыжок без авторизации!)
        [RelayCommand]
        public void AdminEnterMessenger()
        {
            if (CurrentUser == null) return;

            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
        }

        // 🔙 КНОПКА В МЕССЕНДЖЕРЕ (Видна ТОЛЬКО если вошел админ): "Вернуться в Админку"
        [RelayCommand]
        public void AdminReturnToAdmin()
        {
            if (CurrentUser == null) return;

            CurrentMainZone = MainTab.AdminZone;

            if (CurrentAdminTab == AdminSubTab.None)
            {
                CurrentAdminTab = AdminSubTab.LogPanel; // Маленькая страховка
            }
        }

        // Умный геттер для XAML: видна ли кнопка "[ ВЕРНУТЬСЯ В АДМИНКУ ]"?
        public bool IsAdminReturnButtonVisible
        {
            get
            {
                if (CurrentUser == null) return false;
                if (CurrentMainZone != MainTab.ClientZone || CurrentClientTab != ClientSubTab.Messenger) return false;

                return CurrentUser.Email.Contains("admin", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
