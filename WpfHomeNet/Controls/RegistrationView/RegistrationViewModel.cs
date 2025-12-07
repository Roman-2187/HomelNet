
using HomeNetCore.Services.UsersServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static HomeNetCore.Services.UsersServices.RegisterService;

namespace WpfHomeNet.Controls.RegistrationView
{
    public class RegistrationViewModel : ObservableObject
    {
        private readonly RegisterService _registerService;

        // Свойства для данных пользователя
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetField(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }





        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set => SetField(ref _userName, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetField(ref _phone, value);
        }

        // Свойства для отображения результата
        private ValidationState _validationState = ValidationState.Success;
        public ValidationState ValidationState
        {
            get => _validationState;
            set => SetField(ref _validationState, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public ICommand RegisterCommand { get; }

        public RegistrationViewModel(RegisterService registerService)
        {
            if (registerService is null)
                throw new ArgumentNullException(nameof(registerService));

            _registerService = registerService;

            RegisterCommand = new RelayCommand(async (obj) =>
            {
                await ExecuteRegisterCommand();
            }, (obj) =>
            {
                // Проверяем, что все поля заполнены
                return !string.IsNullOrEmpty(Email) &&
                       !string.IsNullOrEmpty(Password) &&
                       !string.IsNullOrEmpty(UserName) &&
                       !string.IsNullOrEmpty(Phone);
            });
        }

        private async Task ExecuteRegisterCommand()
        {
            try
            {
                ValidationState = ValidationState.Success;
                ErrorMessage = string.Empty;

                var result = await _registerService.RegisterUserAsync(
                    Email,
                    Password,
                    UserName,
                    Phone);

                // Исправленная проверка состояния
                if (result.State == ValidationState.Error)
                {
                    ValidationState = ValidationState.Error;
                    ErrorMessage = result.Message;
                    return;
                }

                ErrorMessage = result.Message;
            }
            catch (Exception ex)
            {
                ValidationState = ValidationState.Error;
                ErrorMessage = $"Произошла ошибка при регистрации: {ex.Message}";
            }
        }
    }





}
