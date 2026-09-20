using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase
    {
        [ObservableProperty]
        private string _statusText = "Инициализация приложения...";

        // Конструктор принимает чистый интерфейс IEventBus из Ядра и прокидывает в базу через base
        public StatusBarViewModel(IEventBus eventBus, NavigationStateManager navigation) : base(eventBus, navigation)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 1. 🔥 Слушаем новые короткие текстовые статусы строки состояния
            EventBus.Subscribe<IStatusBarViewModel.TextChanged>(async msg =>
                await UpdateStatusAsync(msg.NewStatus));

            // 2. 🔥 Принимаем короткий рекорд обновления таблицы пользователей
            EventBus.Subscribe<IUsersTableViewModel.Refreshed>(msg =>
            {
                StatusText = msg.Users != null
                    ? $"Загружено {msg.Users.Count} пользователей"
                    : "Список пользователей пуст";
            });
        }

        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка пользователей...";
            await Task.Delay(1200);
            StatusText = text;
            await Task.Delay(2000);

            // Публикуем короткий запрос на обновление статуса/счетчика обратно в таблицу
            EventBus.Publish(this, new IUsersTableViewModel.RefreshRequest());
        }
    }
}
