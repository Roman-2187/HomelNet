using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class StatusBarViewModel : FormViewModelBase
    {   
        private string _statusText = "Инициализация...";
   
        private Func<MainViewModel>? _mainVmProvider;

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        
        public StatusBarViewModel(EventBus eventBus) : base(eventBus)

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
                    // Если форма закрылась — просто плавно обновляем дефолтный счётчик пользователей
                    await RefreshDefaultStatusAsync();
                }
            });

            // 3. Ловим успешную загрузку базы данных из асинхронного потока
            _eventBus.Subscribe<UsersListRefreshedMessage>(async msg =>
            {
                // Вместо провайдера берём Count ПРЯМО ИЗ СООБЩЕНИЯ, которое прислала модель!
                if (msg.Users != null)
                {
                    StatusText = $"Загружено {msg.Users.Count} пользователей";
                }
                else
                {
                    await RefreshDefaultStatusAsync();
                }
            });
        }



        // ИСПРАВЛЕНИЕ: Статус-бар официально пинает метод InitializeAsync главной модели!
        public async void InitializeMainVmProvider(Func<MainViewModel> mainVmProvider)
        {
            _mainVmProvider = mainVmProvider;

            try
            {
                var mainVm = _mainVmProvider.Invoke();
                if (mainVm != null)
                {
                    // ЗАПУСКАЕМ КОНВЕЙЕР: Даем команду главной модели считать СУБД!
                    await mainVm.InitializeAsync();
                }
            }
            catch (Exception )
            {
                StatusText = "Ошибка запуска базы данных ❌";
            }
        }

        private async Task UpdateStatusAsync(string text)
        {
            StatusText = "Загрузка...";
            await Task.Delay(500);
            StatusText = text;
            await Task.Delay(2500);
            await RefreshDefaultStatusAsync();
        }

        private Task RefreshDefaultStatusAsync()
        {
            // Пробиваемся напрямую в живую коллекцию MainViewModel и забираем реальный Count!
            int actualCount = _mainVmProvider?.Invoke()?.Users?.Count ?? -1;
            StatusText = $"Загружено {actualCount} пользователей";
            return Task.CompletedTask;
        }
    }
}
