using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; 

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase
    {
        [ObservableProperty]
        private string _statusText = "Инициализация приложения...";

        // 🔥 Конструктор принимает чистый интерфейс IEventBus из Ядра и прокидывает в базу через base
        public StatusBarViewModel(IEventBus eventBus) : base(eventBus)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 1. 🔥 ПОПРАВИЛИ: Слушаем новые короткие текстовые статусы строки состояния
            EventBus.Subscribe<IStatusBarViewModel.TextChanged>(async msg =>
                await UpdateStatusAsync(msg.NewStatus));

            // 2. 🔥 ПОПРАВИЛИ: Слушаем открытие/закрытие форм через базовый интерфейс
            EventBus.Subscribe<IFormViewModelBase.VisibilityChanged>(async msg =>
            {
                string formFriendlyName = msg.FormType.Name switch
                {
                    "DeleteUsersViewModel" => "Удаление пользователей",
                    "RegistrationViewModel" => "Регистрация",
                    "AuthenticationViewModel" => "Авторизация",
                    _ => "Форма"
                };

                if (msg.IsVisible)
                {
                    await UpdateStatusAsync($"Открыта форма: {formFriendlyName}");
                }
                else
                {
                    await UpdateStatusAsync("Система готова к работе");
                }
            });

            // 3. 🔥 ПОПРАВИЛИ: Принимаем короткий рекорд обновления таблицы пользователей
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

            // 🔥 ПОПРАВИЛИ: Публикуем короткий запрос на обновление статуса/счетчика обратно в таблицу
            EventBus.Publish(this, new IUsersTableViewModel.RefreshRequest());
        }
    }
}
