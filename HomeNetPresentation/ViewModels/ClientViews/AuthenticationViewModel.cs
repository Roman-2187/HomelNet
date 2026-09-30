using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
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
    /// <summary>
    /// Стерильная форма авторизации мессенджера SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера пользователя.
    /// </summary>
    public partial class AuthenticationViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IAuthenticateService _loginService;
        private readonly ILogger _logger;

        [ObservableProperty] private UserEntity _userData = new();

        public AuthenticationViewModel(
            IAuthenticateService loginService,
            IEventBus eventBus,
            ILogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation) // Передали строго типизированный навигатор в generic-базу
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // В конструкторе меняем подписку, чтобы форма сбрасывалась при успешном входе:
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserAuthenticated);

           
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        /// <summary>
        /// Локальный сброс полей формы после подтверждения успешного входа
        /// </summary>

        private async void OnUserAuthenticated(IUserVm.UserSignedIn msg)
        {
            await Task.Delay(500);
            ResetForm();
            InitializeInitialHints();
        }

        #endregion

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
                    _logger.LogInfo($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                    var finalUser = verdict.User ?? UserData;

                    _eventBus.Publish(this, new IUserVm.UserSignedIn(finalUser));
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

            // 🔥 ЧИСТОТА: Отправляем реактивный рекорд отмены в шину событий. 
            // Навигатор поймает его и мгновенно сбросит стейт в абсолютный 0 гостевой зоны.
            _eventBus.Publish(this, new IUserVm.BackToStart());
        }

        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Чистим базовые ресурсы
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserAuthenticated);
        }

        #endregion
    }
}
