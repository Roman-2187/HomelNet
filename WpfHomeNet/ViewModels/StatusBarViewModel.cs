using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class StatusBarViewModel : FormViewModelBase
    {
        private readonly EventBus _eventBus;
        private string _statusText = "Инициализация...";
        private int _usersCount;
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        public StatusBarViewModel(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 1. Слушаем прямые текстовые статусы (например, логаут)
            _eventBus.Subscribe<StatusTextChangedMessage>(async msg =>
                await UpdateStatusAsync(msg.NewStatus));
        
            // 2. Слушаем открытие/закрытие форм напрямую без посредничества MainViewModel!
            _eventBus.Subscribe<FormVisibilityChangedMessage>(async msg =>
            {          
                string formFriendlyName = msg.FormType.Name switch
                {
                    "DeletionUsersViewModel" => "Удаление пользователей",
                    "RegistrationViewModel" => "Регистрация",
                    "AuthenticationViewModel" => "Авторизация",
                    _ => "Форма"
                };

                string text = msg.Visibility == Visibility.Visible
                    ? $"Открыта форма: {formFriendlyName}"
                    : $"Cold close: {formFriendlyName}";

                await UpdateStatusAsync(text);
            });

            // 3. Слушаем СУБД: успешную загрузку базы
            _eventBus.Subscribe<UsersListRefreshedMessage>(async msg =>
            {
                _usersCount = msg.Users.Count;
                await UpdateStatusAsync("Инициализация пользователей успешна");
            });

            // 4. Слушаем удаление юзера
            _eventBus.Subscribe<UserDeletedMessage>(async msg =>
            {
                _usersCount--; // Уменьшаем счетчик на лету
                await UpdateStatusAsync($"Пользователь [ID: {msg.UserId}] удален из базы");
            });

            // 5. Слушаем добавление юзера
            _eventBus.Subscribe<UserAddedMessage>(async msg =>
            {
                _usersCount++; // Увеличиваем счетчик
                await UpdateStatusAsync($"Пользователь {msg.User?.FirstName} успешно добавлен");
            });
        }

        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка...";
            await Task.Delay(500);
            StatusText = text;
            await Task.Delay(2500);
            StatusText = $"Загружено {_usersCount} пользователей";
        }
    }
}

