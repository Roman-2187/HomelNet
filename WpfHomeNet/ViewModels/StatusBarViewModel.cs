using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel; // Нано-движок от Microsoft ✨
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    // ОБЯЗАТЕЛЬНО делаем класс partial, чтобы Студия дорисовала кишки! 🧼
    public partial class StatusBarViewModel : FormViewModelBase
    {
        #region Поля и состояние 🦾

        [ObservableProperty]
        private string _statusText = "Инициализация..."; // Сгенерирует: public string StatusText

        // Локальный счётчик для статус-бара, чтобы не дёргать чужие ВьюМодели 🧼
        private int _lastCount = 0;
        #endregion

        #region Конструктор
        public StatusBarViewModel(EventBus eventBus) : base(eventBus)
        {
            InitializeBusSubscriptions();
        }
        #endregion

        #region Инициализация подписок шины (Только через EventBus!)
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
                    "DeletionUsersViewModel" => "Удаление пользователей",
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
                    await RefreshDefaultStatusAsync();
                }
            });

            // 3. Ловим успешную загрузку базы данных (первичный прогрев счётчика)
            _eventBus.Subscribe<UsersListRefreshedMessage>(async msg =>
            {
                if (msg.Users != null)
                {
                    _lastCount = msg.Users.Count; // Запомнили количество
                    StatusText = $"Загружено {_lastCount} пользователей";
                }
                else
                {
                    await RefreshDefaultStatusAsync();
                }
            });

            // 4. Ловим добавление пользователя — увеличиваем локальный счётчик 🚀
            _eventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User != null)
                {
                    _lastCount++;
                    _ = RefreshDefaultStatusAsync(); // Обновляем дефолтный текст
                }
            });

            // 5. Ловим удаление пользователя — уменьшаем локальный счётчик 🚀
            _eventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                _lastCount = Math.Max(0, _lastCount - 1); // Защита от минуса
                _ = RefreshDefaultStatusAsync(); // Обновляем дефолтный текст
            });
        }
        #endregion

        #region Логика работы

        // Метод инициализации провайдера больше НЕ НУЖЕН и удалён! Полная свобода! 🎉

        // Асинхронное обновление статуса (Твоя пишущая машинка!)
        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка...";
            await Task.Delay(500);
            StatusText = text;
            await Task.Delay(2500);
            await RefreshDefaultStatusAsync();
        }

        // Возврат к дефолтному счётчику пользователей (теперь берёт данные из памяти!)
        private Task RefreshDefaultStatusAsync()
        {
            StatusText = $"Загружено {_lastCount} пользователей";
            return Task.CompletedTask;
        }
        #endregion
    }
}


