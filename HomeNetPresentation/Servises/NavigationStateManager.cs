using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    public partial class NavigationStateManager : ObservableObject
    {
        // 🔥 СТАРТОВЫЙ ХАБ: Начинаем с абсолютного, стерильного нуля везде!
        [ObservableProperty] private MainTab _currentMainZone = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;
        [ObservableProperty] private UserEntity? _currentUser;

        public NavigationStateManager(IEventBus eventBus)
        {
            // 👤 ЮЗЕР ПРИШЕЛ
            eventBus.Subscribe<IAuthenticationViewModel.UserLogged>(msg =>
            {
                CurrentUser = msg.User;
                CurrentMainZone = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger;
                CurrentAdminTab = AdminSubTab.None;
            });

            eventBus.Subscribe<IUsersTableViewModel.Added>(msg =>
            {
                CurrentUser = msg.User;
                CurrentMainZone = MainTab.ClientZone;
                CurrentClientTab = ClientSubTab.Messenger;
                CurrentAdminTab = AdminSubTab.None;
            });

            // 🔥 ВОТ ОН — НАШ ЕДИНЫЙ ПЕРЕХВАТЧИК ЗОН ВНУТРИ КОМАНДИРА НАВИГАЦИИ!
            eventBus.Subscribe<IMainViewModel.ZoneChanged>(msg =>
            {
                // Теперь сам навигатор переключает свои макро-зоны по сигналу из шины!
                CurrentMainZone = msg.TargetTab;
            });
        }


        #region 🧠 РЕАКТИВНЫЕ ХУКИ (Контролируют зачистку взаимных исключений) 🧼

        /// <summary>
        /// Контроль макро-зон. Если ушли на старт — гасим рабочие подпанели.
        /// </summary>
        partial void OnCurrentMainZoneChanged(MainTab value)
        {
            if (value == MainTab.None)
            {
                // На стартовом экране никакого мессенджера или панелей логов быть не может!
                _currentAdminTab = AdminSubTab.None;
                // Не зануляем CurrentClientTab здесь, чтобы дать пользователю открывать формы входа/регистрации на старте!
            }
        }

        #endregion

        // ====== 🦾 РУЧНОЕ УПРАВЛЕНИЕ ШАПКИ (Твоя двухэтапная логика) ======

        // 🛠️ ПРЯМОЙ ВХОД АДМИНА (В 1 клик): Сразу летит на свой экран
        [RelayCommand]
        public void OpenAdminZone()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.None; 
            CurrentClientTab = ClientSubTab.None;   // Гасим клиента
        }

        // 👤 ЮЗЕР: ЭТАП 1 (Выбор формы). Переключает табы, оставаясь на стартовом экране (MainTab.None)
        [RelayCommand]
        public void NavigateInClient(ClientSubTab subTab)
        {
            // Разрешаем открывать формы только если мы не авторизованы (находимся в макро-ноле)
            if (CurrentMainZone == MainTab.None)
            {
                CurrentClientTab = subTab;
            }
        }

        // Ручной возврат из админки обратно в чат (если админ хочет почитать сообщения)
        [RelayCommand]
        public void ReturnToMessenger()
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            CurrentAdminTab = AdminSubTab.None;
        }

        // Навигация админа по внутренним кнопкам своей панели
        [RelayCommand]
        public void NavigateInAdmin(AdminSubTab subTab)
        {
            if (CurrentMainZone != MainTab.AdminZone) return;
            CurrentAdminTab = subTab;
        }

        // 🧼 ТОТАЛЬНЫЙ СБРОС (Кнопка "↪ Выход" возвращает в абсолютный ноль)
        [RelayCommand]
        public void ResetToGuest()
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.None;
            CurrentClientTab = ClientSubTab.None; // Абсолютный чистый холст на старте!
            CurrentAdminTab = AdminSubTab.None;
        }

        // Кнопка в админке для прыжка в мессенджер
        [RelayCommand]
        public void AdminEnterMessenger()
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
        }

        // Кнопка мессенджера для возврата в админку
        [RelayCommand]
        public void AdminReturnToAdmin()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.LogPanel;
        }

        



        // Умный геттер для XAML: видна ли кнопка "[ ВЕРНУТЬСЯ В АДМИНКУ ]"?
        public bool IsAdminReturnButtonVisible
        {
            get
            {
                // 🛡️ ЖЕСТКАЯ ЗАЩИТА: Если пользователя нет, или мы не в чате — мгновенно гасим кнопку
                if (CurrentUser == null) return false;
                if (CurrentMainZone != MainTab.ClientZone || CurrentClientTab != ClientSubTab.Messenger) return false;

                // 🔥 ПОПРАВИЛИ: Безопасный вызов через ?. предотвращает любые предупреждения компилятора!
                return CurrentUser.Email?.Contains("admin", StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }

    }
}
