using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel; // Нано-движок от Microsoft ✨
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    // ОБЯЗАТЕЛЬНО делаем класс partial, чтобы Студия дорисовала кишки! 🧼
    public partial class StatusBarViewModel : FormViewModelBase
    {
        #region Поля (Автоматический штамповочный цех свойств) 🦾

        [ObservableProperty]
        private string _statusText = "Инициализация..."; // Сгенерирует: public string StatusText

        private Func<MainViewModel>? _mainVmProvider; //
        #endregion

        #region Конструктор
        public StatusBarViewModel(EventBus eventBus) : base(eventBus) //
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

            // 3. Ловим успешную загрузку базы данных
            _eventBus.Subscribe<UsersListRefreshedMessage>(async msg =>
            {
                if (msg.Users != null)
                {
                    // Пишем строго в свойство с Большой буквы! Генератор сам пнёт XAML 🧼
                    StatusText = $"Загружено {msg.Users.Count} пользователей";
                }
                else
                {
                    await RefreshDefaultStatusAsync();
                }
            });
        }
        #endregion

        #region Логика работы
        // Метод инициализации провайдера
        public async void InitializeMainVmProvider(Func<MainViewModel> mainVmProvider)
        {
            _mainVmProvider = mainVmProvider;

            try
            {
                var mainVm = _mainVmProvider.Invoke();
                if (mainVm != null)
                {
                    await mainVm.InitializeAsync();
                }
            }
            catch (Exception)
            {
                StatusText = "Ошибка запуска базы данных ❌";
            }
        }

        // Асинхронное обновление статуса (Твоя пишущая машинка!)
        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка...";
            await Task.Delay(500);
            StatusText = text;
            await Task.Delay(2500);
            await RefreshDefaultStatusAsync();
        }

        // Возврат к дефолтному счётчику пользователей
        private Task RefreshDefaultStatusAsync()
        {
            int actualCount = _mainVmProvider?.Invoke()?.Users?.Count ?? -1;
            StatusText = $"Загружено {actualCount} пользователей";
            return Task.CompletedTask;
        }
        #endregion
    }
}

