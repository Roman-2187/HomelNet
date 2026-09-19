using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // 🔥 Подключили интерфейсы Ядра с новыми рекордами
using HomeNetCore.Models;

// 🔥 Жёсткий алиас: используем только твою модель валидации во избежание неоднозначности!
using ValidationResult = HomeNetCore.Models.Validation.ValidationResult;

namespace HomeNetPresentation.ViewModels
{
    public partial class AuthenticationViewModel : FormViewModelBase
    {
        private readonly IAuthenticateService _loginService;
        private readonly ILogger _logger;

        // 🔥 НАША ВИТРИНА: Сюда напрямую биндятся Почта и Пароль из XAML
        [ObservableProperty] private UserEntity _userData = new();

        // Конструктор принимает чистые интерфейсы Ядра и передает шину в базу через base(eventBus)
        public AuthenticationViewModel(IAuthenticateService loginService, IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();

            // 🔥 УЛЕТАЕМ ПОСЛЕ ВХОДА + ПОЛНАЯ ОЧИСТКА ФОРМЫ
            // Слушаем новый короткий рекорд из интерфейса-хозяина!
            EventBus.Subscribe<IAuthenticationViewModel.UserLogged>(async msg =>
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
                // 🔥 ПОПРАВИЛИ: Принимаем красивый вложенный Verdict вместо старого сырого кортежа
                IAuthenticateService.Verdict verdict = await _loginService.CheckUserAsync(UserData);

                IsComplete = verdict.IsSuccess;
                var loggedUser = verdict.User;

                // Переводим список результатов в словарь полей
                ValidationResults = verdict.Messages.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вход выполнен успешно";
                    SubmitButtonText = "Входим...";

                    _logger.LogInformation($"[AuthVM] Пользователь {loggedUser?.Email ?? UserData.Email} успешно авторизован.");

                    // 🔥 ПОПРАВИЛИ: Публикуем новое короткое и лаконичное сообщение в автобус
                    EventBus.Publish(this, new IAuthenticationViewModel.UserLogged(loggedUser ?? UserData));
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
