using HomeNetCore.Data.Enums;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
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

        public async Task<(bool IsSuccess, List<ValidationResult> Messages)> RegisterUserAsync(CreateUserInput userInput)
        {
            // 1. Валидация
            var validationResults = await ValidateInputAsync(userInput);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return (false, validationResults);

            // 2. Создание модели
            var user = CreateUserEntity(userInput);

            // 3. Сохранение
            try
            {
                await _userService.AddUserAsync(user);
                return (true, validationResults);
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult
                {
                    State = ValidationState.Error,
                    Message = $"Ошибка сохранения пользователя: {ex.Message}"
                };
                return (false, new List<ValidationResult> { errorResult });
            }
        }






        private async Task<List<ValidationResult>> ValidateInputAsync(CreateUserInput input)
        {
            var results = new List<ValidationResult>();

            results.Add(ValidatePassword(input.Password));
            results.Add(ValidateUserName(input.UserName));
            results.Add(ValidatePhone(input.PhoneNumber));

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
                PhoneNumber = input.PhoneNumber,
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

        private ValidationResult ValidatePhone(string phone)
        {
            var result = new ValidationResult { Field = TypeField.PhoneType };

            if (string.IsNullOrWhiteSpace(phone))
            {
                result.State = ValidationState.Error;
                result.Message = "Номер телефона не может быть пустым";
                return result;
            }

            if (!_validateField.ValidatePhoneFormat(phone))
            {
                result.State = ValidationState.Error;
                result.Message = "Некорректный формат телефона (допустимо: +79991234567)";
                return result;
            }

            result.State = ValidationState.Success;
            result.Message = "Номер телефона принят";
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
