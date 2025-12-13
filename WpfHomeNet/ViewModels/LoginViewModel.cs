using HomeNetCore.Data.Enums;
using HomeNetCore.Services;
using HomeNetCore.Services.AuthenticationService;
using HomeNetCore.Services.UsersServices;
using System.Windows;
using System.Windows.Input;


namespace WpfHomeNet.ViewModels
{
    public class LoginViewModel :FormViewModelBase
    {
        #region Поля и переменные
        private readonly AuthenticateService _loginService;
        private readonly UserService _userService;

        public AuthenticatesUserInput UserData { get; set; } = new();
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

       
        #endregion

        public LoginViewModel(UserService userService)
        {
            ControlVisibility = Visibility.Collapsed;
           
            _userService = userService;
            _loginService = new AuthenticateService(_userService);  SubmitButtonText = "Войти";

            InitializeInitialHints();

            LoginCommand = new RelayCommand(
                execute: async (obj) => await ExecuteLoginCommand(),
                canExecute: (obj) => true
            );

            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetLoginForm();
                    ControlVisibility = Visibility.Collapsed;
                },
                canExecute: (obj) => true
            );
        }

       

        #region Методы формы входа

        private void InitializeInitialHints()
        {
            var initialHints = new List<ValidationResult>
            {
                new(TypeField.EmailType, "Введите email", ValidationState.Info, true),
                new(TypeField.PasswordType, "Пароль от 6 символов", ValidationState.Info, true)
            };



            UpdateValidation(initialHints);

          
        }

        

        private void ResetLoginForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            AreFieldsEnabled = true;
            SubmitButtonText = "Войти";
        }

        private async Task ExecuteLoginCommand()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                var (isSuccess, messages) = await _loginService.CheckUserAsync(UserData);
                ValidationResults = messages.ToDictionary(r => r.Field, r => r);

                if (isSuccess)
                {
                    StatusMessage = "Вход выполнен успешно";
                    AreFieldsEnabled = false;
                    SubmitButtonText = "Выйти";
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
                AreFieldsEnabled = true;
            }
        }

        #endregion

       
    }

   
}

