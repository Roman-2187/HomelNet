using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetCore.Services;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class AuthenticationViewModel : FormViewModelBase
    {
        #region Поля и переменные
        private readonly AuthenticateService _loginService;

        public UserEntity UserData { get; set; } = new();
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public RelayCommand ToggleRegistrationCommand { get; private set; }
        #endregion

        public AuthenticationViewModel(AuthenticateService loginService, EventBus eventBus) : base(eventBus)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));

            InitializeInitialHints();

            // 🔥 УЛЕТАЕМ ПОСЛЕ ВХОДА + ПОЛНАЯ ОЧИСТКА ФОРМЫ
            _eventBus.Subscribe<UserLoggedMessage>(async msg =>
            {
                // Спокойно ждем 1 секунду в фоновом потоке
                await Task.Delay(1000);

                // Возвращаемся в UI-поток для безопасного изменения интерфейса
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Сначала полностью сбрасываем форму и подсказки, чтобы при следующем открытии всё было чисто
                    ResetForm();
                    InitializeInitialHints();

                    // Хлопаем само окно
                    ControlVisibility = Visibility.Collapsed;
                });
            });

            // Команда "Вход" (на всякий случай, если где-то используется в коде)
            LoginCommand = new RelayCommand(
                async (obj) => await ExecuteLoginCommand(),
                (obj) => !IsComplete
            );

            // Кнопка отмены — мгновенно сбрасывает данные и закрывает панель
            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetForm();
                    InitializeInitialHints();
                    ControlVisibility = Visibility.Collapsed;
                },
                canExecute: (obj) => true
            );

            // Твоя основная команда, к которой привязана кнопка в XAML
            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) =>
                {
                    // Так как повторные клики ты намертво закрыл в XAML через IsEnabled,
                    // этот метод гарантированно вызовется только один раз при валидации
                    if (!IsComplete)
                    {
                        await ExecuteLoginCommand();
                    }
                },
                canExecute: (parameter) => !IsComplete
            );
        }

        private void InitializeInitialHints()
        {
            var initialHints = new List<ValidationResult>
            {
                new(TypeField.EmailType, "Введите email", ValidationState.Info, true),
                new(TypeField.PasswordType, "Текущий пароль", ValidationState.Info, true)
            };
            UpdateValidation(initialHints);

            StatusMessage = string.Empty;
        }

        private void ResetForm()
        {
            UserData = new();
            OnPropertyChanged(nameof(UserData));
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            SubmitButtonText = "войти";
            IsComplete = false;
        }

        private async Task ExecuteLoginCommand()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                var (isSuccess, validationList, loggedUser) = await _loginService.CheckUserAsync(UserData);
                IsComplete = isSuccess;

                ValidationResults = validationList.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вход выполнен успешно";

                    // Вместо пустой строки пишем "Входим...", чтобы заблокированная кнопка 
                    // в течение секунды давала красивый текстовый отклик
                    SubmitButtonText = "Входим...";

                    // Публикуем в шину — MainViewModel/Dashboard ловят и открывают приложение,
                    // а подписка в конструкторе выше запускает таймер удаления окна
                    _eventBus.Publish(new UserLoggedMessage(loggedUser ?? UserData));
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях"; // Поправили "in" на нормальное "в" :)
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
            }
        }
    }
}
