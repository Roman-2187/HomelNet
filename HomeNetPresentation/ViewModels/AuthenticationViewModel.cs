using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;             // Контракты интерфейсов IEventBus и ILogger из Ядра 🧼
using HomeNetCore.Messaging;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetServices.Services.Identity;
using HomeNetServices.Services.Messaging;

namespace HomeNetPresentation.ViewModels
{
    public partial class AuthenticationViewModel : FormViewModelBase
    {
        private readonly AuthenticateService _loginService;
        private readonly ILogger _logger;

        // 🔥 НАША ВИТРИНА: Сюда напрямую биндятся Почта и Пароль из XAML
        [ObservableProperty] private UserEntity _userData = new();

        // Конструктор принимает чистые интерфейсы Ядра и передает шину в базу через base(eventBus)
        public AuthenticationViewModel(AuthenticateService loginService, IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // 🔥 УЛЕТАЕМ ПОСЛЕ ВХОДА + ПОЛНАЯ ОЧИСТКА ФОРМЫ
            // Кроссплатформенный возврат в UI-поток обеспечит SynchronizationContext нашей шины Routing! 🛸⚡
            EventBus.Subscribe<UserLoggedMessage>(async msg =>
            {
                // Спокойно ждем 1 секунду в фоновом потоке
                await Task.Delay(1000);

                // Сначала полностью сбрасываем форму и подсказки, чтобы при следующем открытии всё было чисто
                ResetForm();
                InitializeInitialHints();

                // Хлопаем само окно через чистый bool базового класса! 🧼
                IsControlVisible = false;
            });
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
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            SubmitButtonText = "войти";
            IsComplete = false;
        }

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ КНОПОК ВХОДА (CommunityToolkit)

        // Основная команда Входа (привязывается к кнопке в XAML)
        private bool CanExecuteLogin() => !IsComplete;

        [RelayCommand(CanExecute = nameof(CanExecuteLogin))]
        private async Task LoginAsync()
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
                    SubmitButtonText = "Входим...";

                    _logger.LogInformation($"[AuthVM] Пользователь {loggedUser?.Email ?? UserData.Email} успешно авторизован.");

                    // 🔥 Публикуем системное сообщение в автобус через свойство с БОЛЬШОЙ буквы
                    EventBus.Publish(this, new UserLoggedMessage(loggedUser ?? UserData));
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                    _logger.LogWarning($"[AuthVM] Неудачная попытка входа для: {UserData.Email}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
                _logger.LogError($"[AuthVM] Критический сбой авторизации: {ex.Message}");
            }
            finally
            {
                // Принудительно заставляем тулкит перепроверить доступность кнопок
                LoginCommand.NotifyCanExecuteChanged();
            }
        }

        // Кнопка отмены — мгновенно сбрасывает данные и закрывает панель
        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();
            IsControlVisible = false; // Чистый bool вместо Visibility 🧼
        }

        #endregion
    }
}
