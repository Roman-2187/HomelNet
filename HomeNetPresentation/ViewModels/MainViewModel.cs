using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetOrm.Interfaces;
using HomeNetPresentation.Services; // 🔥 Контракт твоего нового Хаба баз (IDbContextContainer)

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public MainViewModel(ILogger logger, IEventBus eventBus, NavigationStateManager navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Намертво бахаем строкой в логгер при старте приложения SiberNet
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");

            // 🎯 РЕАКТИВНАЯ ЛОВУШКА ИСПРАВЛЕНА: 
            // Садимся в засаду и ждем монолитный вложенный рекорд от самого DbContextContainer!
            _eventBus.Subscribe<IDbContextContainer.InfrastructureReady>(OnDatabaseInfrastructureReady);
        }

        // 🦾 КИБЕР-БУДИЛЬНИК ТЕПЕРЬ СРАБОТАЕТ СТРОГО ПО ФЭНШУЮ
        // Когда этот метод проснётся — SQLite и Postgres уже параллельно проверены и полностью готовы!
        private void OnDatabaseInfrastructureReady(IDbContextContainer.InfrastructureReady msg)
        {
            _logger.LogInfo("[MainViewModel] База данных подала сигнал готовности. Пинаем таблицу юзеров на прогрев...");

            // Безопасный рефреш: TableUsersViewModel зайдёт в готовую базу и чисто скачает кэш
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }
    }
}
