using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetCore.Validation;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Чистая форма регистрации мессенджера SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера пользователя.
    /// </summary>
    public partial class RegistrationViewModel : FormViewModelBase<UserNavigationManager>
    {
        private readonly IRegistrationService _registerService;
        private readonly ICastomLogger _logger;
        [ObservableProperty] private UserEntity _userData = new();


        

        public RegistrationViewModel(
            IRegistrationService registerService,
            IEventBus eventBus,
            ICastomLogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation) // Передали строго типизированный навигатор в базу
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();
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
            SubmitButtonText = "Создать ID";
        }

        public void ResetForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
        }

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ КНОПОК РЕГИСТРАЦИИ (CommunityToolkit)


        [RelayCommand]
        private async Task CheckEmailAsync()
        {
            var email = UserData.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            // 🔥 1. СРАЗУ показываем статус "Проверка..."
            // Создаем временный результат
            var checkingResult = new ValidationResult
            {
                Field = TypeField.EmailType,
                State = ValidationState.Info,
                Message = "Проверка..."
            };

            // Создаем новый словарь и кладем туда "Проверка..."
            var checkingDict = new Dictionary<TypeField, ValidationResult>(ValidationResults);
            checkingDict[TypeField.EmailType] = checkingResult;
            ValidationResults = checkingDict;

            // 2. Дергаем сервис (он будет ждать ответа или таймаута)
            var result = await _registerService.CheckEmailOnBlurAsync(email);

            // 3. Создаем ЕЩЕ ОДИН словарь уже с реальным результатом
            var finalDict = new Dictionary<TypeField, ValidationResult>(ValidationResults);
            finalDict[TypeField.EmailType] = result;
            ValidationResults = finalDict;
        }




        [RelayCommand]
        private async Task RegisterAsync()
        {
            StatusMessage = string.Empty;

            try
            {
                // 1. Спрашиваем бэкенд
                IRegistrationService.Verdict verdict = await _registerService.RegisterUserAsync(UserData);

                // 2. ХАРД-РЕЗЕТ ГРАФИКИ: Сначала полностью очищаем словарь и UI от старых хинтов
                ValidationResults = new Dictionary<TypeField, ValidationResult>();
                UpdateValidation(new List<ValidationResult>());

                // 3. Заливаем в словарь чистый результат бэкенда
                ValidationResults = verdict.Results.ToDictionary(r => r.Field, r => r);

                // 4. Проталкиваем результаты бэкенда в UI
                UpdateValidation(verdict.Results);

                if (verdict.IsValid)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "Готово!";
                    _logger.LogInfo($"[RegisterVM] Пользователь {verdict.VerifiedUser?.Email ?? UserData.Email} успешно прошёл СУБД.");

                    var finalUser = verdict.VerifiedUser ?? UserData;

                   

                    // 🔥 Публикуем факт успешного SignUp (Регистрация)
                    _eventBus.Publish(this, new IUserVm.UserSignedUp(finalUser));



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

            // 🔥 ЧИСТОТА: Отправляем реактивный рекорд отмены. Навигатор сам вернет нас в абсолютный ноль гостя
            _eventBus.Publish(this, new IUserVm.BackToStart());
        }

        #endregion
    }
}
