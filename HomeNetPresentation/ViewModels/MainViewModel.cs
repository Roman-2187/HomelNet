using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // Подключаем интерфейсы для рефреша
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus; // Сохраняем шину в поле класса

        public MainViewModel(ILogger logger, IEventBus eventBus, NavigationStateManager navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus)); // Валидируем шину

            // 🔥 БЛИН-ТЕСТ: Заставляем бэкенд намертво бахнуть строкой в логгер при старте!
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");

            // 🦾 КИБЕР-БУДИЛЬНИК: Пинаем автобус прямо со старта главного окна!
            // Этот сигнал поймает TableUsersViewModel через свой метод OnRefreshRequest.
            // Так как флаг _isLoaded еще false, она пойдет в базу и фоном скачает кэш юзеров!
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }
    }
}
