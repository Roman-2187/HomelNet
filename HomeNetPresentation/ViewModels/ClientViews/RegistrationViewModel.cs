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

        public RegistrationViewModel(IRegistrationService registerService,
            IEventBus eventBus,
            ILogger logger,
            NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // 🎯 МЫ ПОЛНОСТЬЮ УДАЛИЛИ ОТСЮДА EventBus.Subscribe<IUsersTableViewModel.Added>!
            // Локальная зачистка полей формы теперь происходит напрямую в методе RegisterAsync.
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

        public void ResetForm()
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

            try
            {
                // 1. Спрашиваем бэкенд
                IRegistrationService.Verdict verdict = await _registerService.RegisterUserAsync(UserData);

                // 2. 🎯 ХАРД-РЕЗЕТ ГРАФИКИ: Сначала полностью очищаем словарь и UI от старых бирюзовых "true"-хинтов!
                ValidationResults = new Dictionary<TypeField, ValidationResult>();
                UpdateValidation(new List<ValidationResult>());

                // 3. Заливаем в словарь чистый результат бэкенда
                ValidationResults = verdict.Results.ToDictionary(r => r.Field, r => r);

                // 4. Проталкиваем результаты бэкенда в UI. Теперь старых хинтов нет, и UI обязан нарисовать новые!
                UpdateValidation(verdict.Results);

                if (verdict.IsValid)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "Готово!";
                    _logger.LogInformation($"[RegisterVM] Пользователь {verdict.VerifiedUser?.Email ?? UserData.Email} успешно прошёл СУБД.");

                    var finalUser = verdict.VerifiedUser ?? UserData;

                    Navigation.SetClientZone(finalUser);
                    EventBus.Publish(this, new IUsersTableViewModel.Added(finalUser));

                    await Task.Delay(500);
                    ResetForm();
                    InitializeInitialHints();
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
            // Публикуем приказ для Навигатора уйти в ноль (Шапка перехватит ответ и закроет экран)
            EventBus.Publish(this, new ITitleBarViewModel.MacroNavigation(MainTab.None));
        }

        #endregion
    }
}
