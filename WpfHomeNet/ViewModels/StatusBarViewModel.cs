using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase
    {
        [ObservableProperty]
        private string _statusText = "Инициализация приложения...";

        public StatusBarViewModel(EventBus eventBus) : base(eventBus) =>InitializeBusSubscriptions();
        

        private void InitializeBusSubscriptions()
        {
            // 1. Слушаем прямые текстовые статусы
            _eventBus.Subscribe<StatusTextChangedMessage>(async msg =>
                await UpdateStatusAsync(msg.NewStatus));

            // 2. Слушаем открытие/закрытие форм
            _eventBus.Subscribe<FormVisibilityChangedMessage>(async msg =>
            {
                string formFriendlyName = msg.FormType.Name switch
                {
                    "DeleteUsersViewModel" => "Удаление пользователей",
                    "RegistrationViewModel" => "Регистрация",
                    "AuthenticationViewModel" => "Авторизация",
                    _ => "Форма"
                };

                if (msg.Visibility == Visibility.Visible)
                {
                    await UpdateStatusAsync($"Открыта форма: {formFriendlyName}");
                }
                else
                {
                    await UpdateStatusAsync("Система готова к работе");
                }
            });

            // 3. ПРИЁМ СЧЁТЧИКА: Таблица прислала живой список — просто выводим каунт! 🚀💎
            _eventBus.Subscribe<UsersListRefreshedMessage>(msg =>
            {
                StatusText = $"Загружено {msg.Users.Count} пользователей";
            });
        }

        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка пользователей...";
            await Task.Delay(1200);
            StatusText = text;
            await Task.Delay(2000);

            // Пишущая машинка закончила? Просим таблицу вернуть счётчик на экран! 👌
            _eventBus.Publish(new RequestStatusRefreshMessage());
        }
    }
}


