using HomeNetCore.Enums;
using HomeNetCore.Models.InputUserData;
using HomeNetCore.Services.UsersServices;

namespace HomeNetCore.Services.AuthenticationService
{

    public class AuthenticateService
    {
        private readonly UserService _userService;
        private readonly ValidationFormat _validateField = new();

        public AuthenticateService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<(bool IsSuccess, List<ValidationResult> Messages)> CheckUserAsync(LoginInUserInput userInput)
        {
            var validationResults = await ValidateInputAsync(userInput);

            var hasCriticalErrors = validationResults.Any(r => r.State == ValidationState.Error);
                              
            return (!hasCriticalErrors, validationResults);
        }

        private async Task<List<ValidationResult>> ValidateInputAsync(LoginInUserInput input)
        {
            var results = new List<ValidationResult>();

            results.Add(await ValidateEmailAsync(input.Email));

            var passwordMatch = await VerifyPasswordPlainTextAsync(input.Email, input.Password);
            results.Add(passwordMatch);

            return results;
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
                    result.State = ValidationState.Success;
                    result.Message = "Email найден";
                }
                else
                {
                    result.State = ValidationState.Error;
                    result.Message = "Email не найден";
                }
            }
            catch (Exception ex)
            {
                result.State = ValidationState.Error;
                result.Message = $"Ошибка проверки email: {ex.Message}";
            }

            return result;
        }

        private async Task<ValidationResult> VerifyPasswordPlainTextAsync(string email, string password)
        {
            var result = new ValidationResult { Field = TypeField.PasswordType };

            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    result.State = ValidationState.Error;
                    result.Message = "Пароль не может быть пустым";
                    return result;
                }

                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                {
                    result.State = ValidationState.Error;
                    result.Message = "Пользователь не найден";
                    return result;
                }

                if (user.Password == password)
                {
                    result.State = ValidationState.Success;
                    result.Message = "Пароль верен";
                }
                else
                {
                    result.State = ValidationState.Error;
                    result.Message = "Неверный пароль";
                }
            }
            catch (Exception ex)
            {
                result.State = ValidationState.Error;
                result.Message = $"Ошибка проверки пароля: {ex.Message}";
            }

            return result;
        }
    }


}
