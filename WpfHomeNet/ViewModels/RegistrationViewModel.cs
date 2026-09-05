using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetCore.Services;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class RegistrationViewModel : FormViewModelBase
    {
        private readonly RegisterService _registerService;
        private UserEntity? _createdUser;

        // 1. Сюда напрямую биндятся Имя, Почта и Пароль
        public UserEntity UserData { get; set; } = new();

        // 2. Изолированное свойство ТОЛЬКО для UI-проверки совпадения (в базу не летит!)
        

       
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleRegistrationCommand { get; }

        public RegistrationViewModel(RegisterService registerService, EventBus eventBus) : base(eventBus)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));

            InitializeInitialHints();

            RegisterCommand = new RelayCommand(
                execute: async (obj) => await ExecuteRegisterCommand()
            );

            CancelCommand = new RelayCommand(
                execute: (obj) => CloseForm()
            );

            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) => await ExecuteRegisterCommand(),
                canExecute: (parameter) => true
            );
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
            OnPropertyChanged(nameof(UserData));
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            InitializeInitialHints();
            ControlVisibility = Visibility.Collapsed;
        }

        private async Task ExecuteRegisterCommand()
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

                    if (_createdUser != null)
                    {
                        _eventBus.Publish(new UserRegisteredMessage(_createdUser));
                    }

                    // 🔥 МАГИЯ АВТОМАТИЗАЦИИ: замираем на 1 секунду и бесшумно схлопываем форму!
                    await Task.Delay(1500);
                    CloseForm();
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
            }
        }
    }
}
