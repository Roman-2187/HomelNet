using HomeNetCore.Data.Enums;
using HomeNetCore.Services;
using HomeNetCore.Services.UsersServices;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    public class RegistrationViewModel :FormViewModelBase
    {
        #region поля и переменные
        private readonly RegisterService _registerService;
        private readonly UserService _userService;
        public CreateUserInput UserData { get; set; } = new();
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleRegistrationCommand { get; }
        #endregion


        public RegistrationViewModel(UserService userService)
        {
           
           _userService = userService;
            _registerService = new RegisterService(_userService);

            InitializeInitialHints();

            RegisterCommand = new RelayCommand(
                execute: async (obj) => await ExecuteRegisterCommand(),
                canExecute: (obj) => true
            );

            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetRegistrationForm();
                    InitializeInitialHints();
                    ControlVisibility = Visibility.Collapsed;
                },
                canExecute: (obj) => true
            );

            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) =>
                {
                    if (!IsComplete)
                        await ExecuteRegisterCommand();
                    else
                    {
                        ResetRegistrationForm();
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
                new(TypeField.EmailType, "Введите email например 'User@example.com'", ValidationState.Info, true),
                new(TypeField.PasswordType, "Пароль должен содержать 8 символов  буквы и цифры", ValidationState.Info, true),
                new(TypeField.NameType, "Имя пользователя должно содержать 3 буквы подряд", ValidationState.Info, true),
                new(TypeField.PhoneType, "Формат: +7 XXX XXX-XX-XX", ValidationState.Info, true)
            };

            UpdateValidation(initialHints);

            SubmitButtonText = "Зарегистрироваться";
        }

        

        private void ResetRegistrationForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            AreFieldsEnabled = true;
            SubmitButtonText = "Зарегистрироваться";
            IsComplete = false;
        }

        private async Task ExecuteRegisterCommand()
        {

            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                var (isSuccess, messages) = await _registerService.RegisterUserAsync(UserData);
                ValidationResults = messages.ToDictionary(r => r.Field, r => r);

                if (isSuccess)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    AreFieldsEnabled = false;
                    IsComplete = true;
                    SubmitButtonText = "Выйти";
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
                AreFieldsEnabled = true;
            }
        }
       
    }

}



