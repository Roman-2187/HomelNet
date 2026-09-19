using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels; // 🔥 Подключили интерфейсы Ядра с новыми рекордами
using HomeNetCore.Models;

// 🔥 Жёсткий алиас: используем только твою модель валидации во избежание неоднозначности!
using ValidationResult = HomeNetCore.Models.Validation.ValidationResult;

namespace HomeNetPresentation.ViewModels
{
    public partial class RegistrationViewModel : FormViewModelBase
    {
        private readonly IRegistrationService _registerService;
        private readonly ILogger _logger;
        private UserEntity? _createdUser;

        // 🔥 НАША ВИТРИНА: Сюда напрямую биндятся Имя, Почта и Пароль из XAML
        [ObservableProperty] private UserEntity _userData = new();

        // Конструктор принимает чистые интерфейсы Ядра и передает шину в базу через base(eventBus)
        public RegistrationViewModel(IRegistrationService registerService, IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // 🔥 ЗАДЕРЖКА ПОСЛЕ РЕГИСТРАЦИИ: Слушаем новый короткий рекорд таблицы пользователей!
            EventBus.Subscribe<IUsersTableViewModel.Added>(async msg =>
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
                // 🔥 ПОПРАВИЛИ: Принимаем красивый вложенный Verdict вместо старого сырого кортежа
                IRegistrationService.Verdict verdict = await _registerService.RegisterUserAsync(UserData);

                IsComplete = verdict.IsValid;
                _createdUser = verdict.VerifiedUser;

                // Переводим список результатов в словарь полей
                ValidationResults = verdict.Results.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "ща погодь!";

                    _logger.LogInformation($"[RegisterVM] Пользователь {_createdUser?.Email ?? UserData.Email} успешно прошёл валидацию СУБД.");

                    // 🔥 ПОПРАВИЛИ: Публикуем новое лаконичное бизнес-сообщение о добавлении юзера
                    EventBus.Publish(this, new IUsersTableViewModel.Added(_createdUser ?? UserData));
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
