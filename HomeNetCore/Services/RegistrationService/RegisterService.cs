using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.InputUserData;
using HomeNetCore.Services.UsersServices;

namespace HomeNetCore.Services
{
   

    public  class RegisterService
    {
        private readonly UserService _userService;
        private readonly ValidationFormat _validateField = new();

        public RegisterService(UserService userService)
        {
           _userService = userService;
        }

        public async Task<(bool IsSuccess, List<ValidationResult> Messages, UserEntity? CreatedUser)> RegisterUserAsync(CreateUserInput userInput)
        {
            // 1. Валидация
            var validationResults = await ValidateInputAsync(userInput);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return (false, validationResults,null);

            // 2. Создание модели
            var user = CreateUserEntity(userInput);

            // 3. Сохранение
            try
            {
                await _userService.AddUserAsync(user);
                return (true, validationResults,user);
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult
                {
                    State = ValidationState.Error,
                    Message = $"Ошибка сохранения пользователя: {ex.Message}"
                };
                return (false, new List<ValidationResult> { errorResult },null);
            }
        }






        private async Task<List<ValidationResult>> ValidateInputAsync(CreateUserInput input)
        {
            var results = new List<ValidationResult>
            {
                ValidatePassword(input.Password),
                ValidateUserName(input.UserName),
                ValidateConfirmedPassword(input.Password, input.ConfirmPassword)
            };

            var emailResult = await ValidateEmailAsync(input.Email);
            results.Add(emailResult);

            return results;
        }


        private UserEntity CreateUserEntity(CreateUserInput input)
        {
            return new UserEntity
            {
                FirstName = input.UserName,
                Email = input.Email,               
                Password = input.Password
            };
        }


        private ValidationResult ValidatePassword(string password)
        {
            var result = new ValidationResult { Field = TypeField.PasswordType };

            if (string.IsNullOrWhiteSpace(password))
            {
                result.State = ValidationState.Error;
                result.Message = "Пароль не может быть пустым";
                return result;
            }

            if (!_validateField.ValidatePasswordFormat(password))
            {
                result.State = ValidationState.Error;
                result.Message = "Пароль должен содержать минимум 8 символов, буквы и цифры";
                return result;
            }

            result.State = ValidationState.Success;
            result.Message = "Пароль принят";
            return result;
        }


        private ValidationResult ValidateConfirmedPassword(string password, string confirmedPassword)
        {
            var result = new ValidationResult { Field = TypeField.ConfirmedPasswordType };

            if (string.IsNullOrWhiteSpace(confirmedPassword))
            {
                result.State = ValidationState.Error;
                result.Message = "пароль  не может быть пустым";
                return result;
            }


            if(confirmedPassword == password)
            {
                result.State = ValidationState.Success;
                result.Message = "пароли совпадают";
                return result; 
            }

            else
            {
                result.State = ValidationState.Error;
                result.Message = "пароли не совпадают";
                return result;
            }


        }


        private ValidationResult ValidateUserName(string userName)
        {
            var result = new ValidationResult { Field = TypeField.NameType };

            if (string.IsNullOrWhiteSpace(userName))
            {
                result.State = ValidationState.Error;
                result.Message = "Имя пользователя не может быть пустым";
                return result;
            }

            if (!_validateField.ValidateUserNameFormat(userName))
            {
                result.State = ValidationState.Error;
                result.Message = "Допустимо минимум 3 буквы подряд без пробелов";
                return result;
            }

            result.State = ValidationState.Success;
            result.Message = "Имя пользователя принято";
            return result;
        }

       
        private async Task<ValidationResult> ValidateEmailAsync(string email)
        {
            var result = new ValidationResult { Field = TypeField.EmailType };

            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    result.State = ValidationState.Error;
                    result.Message = "Email не может быть пустым";
                    return result;
                }

                if (!_validateField.IsValidEmailFormat(email))
                {
                    result.State = ValidationState.Error;
                    result.Message = "Некорректный формат email";
                    return result;
                }

                if (await _userService.CheckEmailExistsAsync(email))
                {
                    result.State = ValidationState.Error;
                    result.Message = "Email уже зарегистрирован";
                    return result;
                }

                result.State = ValidationState.Success;
                result.Message = "Email принят";
                return result;
            }
            catch (Exception ex)
            {
                result.State = ValidationState.Error;
                result.Message = $"Ошибка проверки email: {ex.Message}";
                return result;
            }
        }
    }

}
