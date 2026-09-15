using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;             // Наш чистый контракт ILogger и IEventBus из Ядра 🧼
using HomeNetCore.Messaging;              // Наши чистые сигналы-рекорды из Ядра
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetServices.Services.Identity;
using HomeNetServices.Services.Messaging;

namespace HomeNetPresentation.ViewModels
{
    public partial class RegistrationViewModel : FormViewModelBase
    {
        private readonly RegisterService _registerService;
        private readonly ILogger _logger;
        private UserEntity? _createdUser;

        // 🔥 НАША ВИТРИНА: Сюда напрямую биндятся Имя, Почта и Пароль из XAML
        [ObservableProperty] private UserEntity _userData = new();

        // Конструктор принимает чистые интерфейсы Ядра и передает шину в базу через base(eventBus)
        public RegistrationViewModel(RegisterService registerService, IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // 🔥 ЗАДЕРЖКА ПОСЛЕ РЕГИСТРАЦИИ: плавно улетаем через 1 секунду, не ломая UI-поток!
            // За счёт SynchronizationContext в шине Routing, возврат в UI выполнится кроссплатформенно БЕЗ ДИСПЕТЧЕРОВ! 🛸✨
            EventBus.Subscribe<UserAddedMessage>(async msg =>
            {
                // Даем пользователю 1 секунду порадоваться успеху
                await Task.Delay(1000);

                // Безопасно закрываем форму. Свойство IsControlVisible из базы само уведомит систему!
                CloseForm();
            });
        }

        private void InitializeInitialHints()
        {
            var initialHints = new List<ValidationResult>
            {
                new(TypeField.EmailType, "Введите email например 'User@example.com'", ValidationState.Info, true),
                new(TypeField.PasswordType, "Пароль должен содержать 8 символов буквы и цифры", ValidationState.Info, true),
                new(TypeField.NameType, "Имя пользователя должно содержать 3 буквы подряд", ValidationState.Info, true),
                new(TypeField.ConfirmedPasswordType, "Пароли должны совпадать", ValidationState.Info, true)
            };

            UpdateValidation(initialHints);
            SubmitButtonText = "Зарегистрироваться";
        }

        private void CloseForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            InitializeInitialHints();

            // Заменили Visibility.Collapsed на чистый bool базового класса! 🧼🦾
            IsControlVisible = false;
        }

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ КНОПОК РЕГИСТРАЦИИ (CommunityToolkit)

        // Кнопка "Зарегистрироваться"
        [RelayCommand]
        private async Task RegisterAsync()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                (IsComplete, ValidationResult, _createdUser) = await _registerService.RegisterUserAsync(UserData);
                ValidationResults = ValidationResult.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "ща погодь!";

                    _logger.LogInformation($"[RegisterVM] Пользователь {_createdUser?.Email ?? UserData.Email} успешно прошёл валидацию СУБД.");

                    // 🔥 Публикуем наше системное бизнес-сообщение в автобус через свойство с БОЛЬШОЙ буквы
                    EventBus.Publish(this, new UserAddedMessage(_createdUser ?? UserData));
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                    _logger.LogWarning($"[RegisterVM] Ошибка заполнения полей регистрации для: {UserData.Email}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
                _logger.LogError($"[RegisterVM] Критический сбой регистрации: {ex.Message}");
            }
        }

        // Кнопка "Отмена"
        [RelayCommand]
        private void Cancel() => CloseForm();

        #endregion
    }
}
