using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetCore.Services;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class AuthenticationViewModel : FormViewModelBase
    {
        #region Поля и переменные
        private readonly AuthenticateService _loginService;

        public UserEntity UserData { get; set; } = new();
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public RelayCommand ToggleRegistrationCommand { get; private set; }
        #endregion

        public AuthenticationViewModel(AuthenticateService loginService, EventBus eventBus) : base(eventBus)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));

            InitializeInitialHints();

           

            // 🔥 ЗАДЕРЖКА ПОСЛЕ ВХОДА: плавно улетаем через 1 секунду, не блокируя UI-поток
            _eventBus.Subscribe<UserLoggedMessage>(async msg =>
            {
                // Ждем 1 секунду (или 1500 мс, если хочешь паузу чуть дольше)
                await Task.Delay(1000);

                // Возвращаемся в UI-поток, чтобы безопасно изменить видимость контрола
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ControlVisibility = Visibility.Collapsed;
                });
            });




            // Кнопка доступна для клика и наведения только если IsComplete == false
            LoginCommand = new RelayCommand(
                async (obj) => await ExecuteLoginCommand(),
                (obj) => !IsComplete
            );

            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetForm();
                    InitializeInitialHints();
                    ControlVisibility = Visibility.Collapsed;
                },
                canExecute: (obj) => true
            );

            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) =>
                {
                    if (!IsComplete)
                    {
                        await ExecuteLoginCommand();
                    }
                    else
                    {
                        ResetForm();
                        InitializeInitialHints();
                        ControlVisibility = Visibility.Collapsed;
                    }
                },
                canExecute: (parameter) => !IsComplete || true
            );
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
            OnPropertyChanged(nameof(UserData));
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            SubmitButtonText = "войти";
            IsComplete = false;
        }

        private async Task ExecuteLoginCommand()
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
                    SubmitButtonText = string.Empty;

                    // Публикуем сообщение об успехе — дашборд его поймает и вылетит снизу,
                    // а этот класс благодаря подписке выше поймает его и плавно улетит вверх.
                    _eventBus.Publish(new UserLoggedMessage(loggedUser ?? UserData));


                }
                else
                {
                    StatusMessage = "Есть ошибки in полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
            }
        }
    }
}
