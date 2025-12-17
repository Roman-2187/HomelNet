using HomeNetCore.Enums;
using HomeNetCore.Models.InputUserData;
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

        public LoginInUserInput UserData { get; set; } = new();
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public RelayCommand ToggleRegistrationCommand { get; private set; }
        #endregion


        public LoginViewModel(UserService userService)
        {
            _userService = userService;
            _loginService = new AuthenticateService(_userService);

            InitializeInitialHints();
           
            LoginCommand = new RelayCommand(
               execute: async (obj) => await ExecuteLoginCommand(),
               canExecute: (obj) => true
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
                        await ExecuteLoginCommand();
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
                 (IsComplete, ValidationResult) = await _loginService.CheckUserAsync(UserData);
                ValidationResults = ValidationResult.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вход выполнен успешно";                  
                    SubmitButtonText = "OK";
                    IsComplete = true;
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
               
            }
        }   
    }  
}

