using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class AuthenticationViewModel : FormViewModelBase
    {
        private readonly IAuthenticateService _loginService;
        private readonly ILogger _logger;
        // 🧠 Сохраняем прямую ссылку на наш манипулятор состояний
        private readonly NavigationStateManager _navigationStateManager;

        [ObservableProperty] private UserEntity _userData = new();

        public AuthenticationViewModel(IAuthenticateService loginService,
            IEventBus eventBus, 
            ILogger logger, 
            NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationStateManager = navigation ?? throw new ArgumentNullException(nameof(navigation));

            InitializeInitialHints();

            // Локальная зачистка полей после успешного входа (автобус теперь дергаем только для этого!)
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg =>
            {
                await Task.Delay(500); // Небольшая задержка для плавности анимации
                ResetForm();
                InitializeInitialHints();
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

        public void ResetForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            SubmitButtonText = "войти";
        }

        #region 🦾 НАНО-КОМАНДЫ ДЛЯ КНОПОК ВХОДА (CommunityToolkit)

        [RelayCommand]
        private async Task LoginAsync()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                IAuthenticateService.Verdict verdict = await _loginService.CheckUserAsync(UserData);
                ValidationResults = verdict.Messages.ToDictionary(r => r.Field, r => r);

                if (verdict.IsSuccess)
                {
                    StatusMessage = "Вход выполнен успешно";
                    SubmitButtonText = "Входим...";
                    _logger.LogInformation($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                    var finalUser = verdict.User ?? UserData;

                    // 🎯 ШАГ 1: Даем команду Навигатору отправить пользователя в зону чата
                    Navigation.SetClientZone(finalUser);

                    // 🎯 ШАГ 2: Прямо здесь локально сбрасываем поля формы логина, 
                    // чтобы при выходе из аккаунта там не оставался старый пароль!
                    await Task.Delay(500); // Небольшая задержка для плавности киберпанк-анимации
                    ResetForm();
                    InitializeInitialHints();
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
        }


        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();

            // Швыряем приказ в автобус через новый интерфейс шапки! 
            // Инспектор автобуса это мгновенно запишет.
            EventBus.Publish(this, new ITitleBarViewModel.MacroNavigation(MainTab.None));
        }




        #endregion
    }
}
