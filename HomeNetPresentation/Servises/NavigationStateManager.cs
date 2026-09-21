using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Events; // 🎯 Импорт нашего автобуса
using HomeNetCore.Interfaces.ViewModels; // 🎯 Импорт интерфейса ITitleBarViewModel с рекордами
using HomeNetCore.Models;

namespace HomeNetPresentation.Services
{
    public partial class NavigationStateManager : ObservableObject
    {
        private readonly IEventBus _eventBus; // Переменная для шины

        // Наш единственный источник правды для XAML DataTrigger-ов
        [ObservableProperty] private MainTab _currentMainZone = MainTab.None;
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;
        [ObservableProperty] private AdminSubTab _currentAdminTab = AdminSubTab.None;
        [ObservableProperty] private UserEntity? _currentUser;

        // 🦾 Внедряем автобус в конструктор
        public NavigationStateManager(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 🎯 СЛУШАЕМ АВТОБУС: Когда прилетает приказ на смену макро-зоны
            _eventBus.Subscribe<ITitleBarViewModel.MacroNavigation>(msg =>
            {
                if (msg.TargetTab == MainTab.None)
                {
                    SetStartZone();
                }
                else if (msg.TargetTab == MainTab.AdminZone)
                {
                    SetAdminZone();
                }
            });

            // 🎯 СЛУШАЕМ АВТОБУС: Если прилетит точечный приказ на смену только суб-таба
            _eventBus.Subscribe<ITitleBarViewModel.ClientTabChanged>(msg =>
            {
                SetAuthZone(msg.TargetSubTab);
            });
        }

        // Включаем режим: Стартовая страница (Абсолютный ноль)
        public void SetStartZone()
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = ClientSubTab.None;
            CurrentAdminTab = AdminSubTab.None;

            // 📢 Стреляем ответом: Навигатор всё переключил, шапка — обновляй UI!
            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Форма авторизации (Вход / Регистрация)
        public void SetAuthZone(ClientSubTab formTab)
        {
            CurrentMainZone = MainTab.None;
            CurrentClientTab = formTab;
            CurrentAdminTab = AdminSubTab.None;

            // 📢 Стреляем ответом в автобус
            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Клиент зашел в чат
        public void SetClientZone(UserEntity user)
        {
            CurrentUser = user;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            CurrentAdminTab = AdminSubTab.None;

            // 📢 Стреляем ответом в автобус
            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Включаем режим: Админ панель (Главный экран, вкладки очищены)
        public void SetAdminZone()
        {
            CurrentMainZone = MainTab.AdminZone;
            CurrentAdminTab = AdminSubTab.None;
            CurrentClientTab = ClientSubTab.None;

            // 📢 Стреляем ответом в автобус
            _eventBus.Publish(this, new ITitleBarViewModel.ZoneChanged(CurrentMainZone, CurrentClientTab));
        }

        // Внутренний тумблер Админки: Включение под-экранов (Таблица, Логи, Удаление)
        public void ToggleAdminSubTab(AdminSubTab targetTab)
        {
            if (CurrentMainZone != MainTab.AdminZone) return;

            CurrentAdminTab = CurrentAdminTab == targetTab ? AdminSubTab.None : targetTab;

            // Если для админских под-вкладок будет отдельный триггер в XAML,
            // можно будет отправлять еще один специализированный Publish
        }

        // Полный сброс при выходе
        public void ClearToGuest()
        {
            CurrentUser = null;
            SetStartZone(); // Внутри автоматически сработает Publish
        }

        // Умный геттер для видимости кнопки возврата (остается без изменений)
        public bool IsAdminReturnButtonVisible =>
            CurrentUser != null &&
            CurrentMainZone == MainTab.ClientZone &&
            CurrentClientTab == ClientSubTab.Messenger &&
            (CurrentUser.Email?.Contains("admin", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
