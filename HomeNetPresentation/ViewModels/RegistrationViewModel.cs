using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class RegistrationViewModel : FormViewModelBase
    {
        private readonly IRegistrationService _registerService;
        private readonly ILogger _logger;

        [ObservableProperty] private UserEntity _userData = new();

        public RegistrationViewModel(IRegistrationService registerService, IEventBus eventBus, ILogger logger, NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // Слушаем таблицу пользователей: при успешном создании тихо затираем за собой поля
            EventBus.Subscribe<IUsersTableViewModel.Added>(async msg =>
            {
                await Task.Delay(500);
                ResetForm();
                InitializeInitialHints();
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

        public  void ResetForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
        }

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ КНОПОК РЕГИСТРАЦИИ (CommunityToolkit)

        [RelayCommand]
        private async Task RegisterAsync()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                IRegistrationService.Verdict verdict = await _registerService.RegisterUserAsync(UserData);
                ValidationResults = verdict.Results.ToDictionary(r => r.Field, r => r);

                if (verdict.IsValid)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "Готово!";
                    _logger.LogInformation($"[RegisterVM] Пользователь {verdict.VerifiedUser?.Email ?? UserData.Email} успешно прошёл СУБД.");

                    // Публикуем добавление, навигатор поймает и в два этапа пропихнет юзера в мессенджер! ✨
                    EventBus.Publish(this, new IUsersTableViewModel.Added(verdict.VerifiedUser ?? UserData));
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

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();
            // Отмена уводит роутер обратно в чистый ноль стартового экрана
            EventBus.Publish(this, new IMainViewModel.ZoneChanged(MainTab.None));
        }

        #endregion
    }
}
