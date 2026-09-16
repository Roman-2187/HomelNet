using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Events;
using HomeNetCore.Interfaces;

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase
    {
        [ObservableProperty]
        private string _statusText = "Инициализация приложения...";

        // 🔥 ИСПРАВЛЕНО: Конструктор принимает чистый интерфейс IEventBus из Ядра и прокидывает в базу через base
        public StatusBarViewModel(IEventBus eventBus) : base(eventBus)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 1. Слушаем прямые текстовые статусы через базовое свойство EventBus с БОЛЬШОЙ буквы! 🧼⚡
            EventBus.Subscribe<StatusTextChangedMessage>(async msg =>
                await UpdateStatusAsync(msg.NewStatus));

            // 2. Слушаем открытие/закрытие форм (перевели на наш новый чистый bool флаг видимости!)
            EventBus.Subscribe<FormVisibilityChangedMessage>(async msg =>
            {
                string formFriendlyName = msg.FormType.Name switch
                {
                    "DeleteUsersViewModel" => "Удаление пользователей",
                    "RegistrationViewModel" => "Регистрация",
                    "AuthenticationViewModel" => "Авторизация",
                    _ => "Форма"
                };

                // Больше никаких Visibility.Visible! Проверяем чистый кроссплатформенный bool 🛸
                if (msg.IsVisible)
                {
                    await UpdateStatusAsync($"Открыта форма: {formFriendlyName}");
                }
                else
                {
                    await UpdateStatusAsync("Система готова к работе");
                }
            });

            // 3. ПРИЁМ СЧЁТЧИКА: Таблица прислала живой список — просто выводим каунт! 🚀💎
            EventBus.Subscribe<UsersListRefreshedMessage>(msg =>
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

            // Пишущая машинка закончила? Просим таблицу вернуть счётчик на экран! 👌
            EventBus.Publish(this, new RequestStatusRefreshMessage());
        }
    }
}
