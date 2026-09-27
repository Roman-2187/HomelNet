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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            // 🔥 ЖЕСТКАЯ ДИСЦИПЛИНА: Никаких анонимных лямбд! Подписываем строго именованный метод для Инспектора.
            _eventBus.Subscribe<IUserVm.UserAuthenticated>(OnUserAuthenticated);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        /// <summary>
        /// Локальный сброс полей формы после подтверждения успешного входа
        /// </summary>
        private async void OnUserAuthenticated(IUserVm.UserAuthenticated msg)
        {
            await Task.Delay(500); // Небольшая задержка для плавности киберпанк-анимации
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
                    _logger.LogInformation($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                    var finalUser = verdict.User ?? UserData;

                    // 🔥 ТОТАЛЬНЫЙ ДЕКУПЛИНГ: Больше никакой ручной отправки зон в навигатор!
                    // Пуляем в автобус наш чистенький рекорд с сущностью залогиненного юзера.
                    // UserNavigationManager сам поймает этот сигнал, подгрузит ID и переключит UI в мессенджер.
                    _eventBus.Publish(this, new IUserVm.UserAuthenticated(finalUser));
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
            _eventBus.Unsubscribe<IUserVm.UserAuthenticated>(OnUserAuthenticated);
        }

        #endregion
    }
}
