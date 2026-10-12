using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetOrm.Interfaces.Builders;
using HomeNetPresentation.Services; // 🔥 Контракт твоего нового Хаба баз (IDbContextContainer)

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public MainViewModel(ILogger logger, IEventBus eventBus, BaseNavigationState navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Намертво бахаем строкой в логгер при старте приложения SiberNet
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");


        }

        // 🦾 КИБЕР-БУДИЛЬНИК ТЕПЕРЬ СРАБОТАЕТ СТРОГО ПО ФЭНШУЮ
        // Когда этот метод проснётся — SQLite и Postgres уже параллельно проверены и полностью готовы!

    }
}
