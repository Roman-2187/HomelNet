using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;



namespace HomeNetServices.Identity
{
        public class AuthenticateService:IAuthenticateService
        {
            private readonly IUserService _userService;
            private readonly ValidationFormat _validateField = new();

            public AuthenticateService(IUserService userService)
            {
                _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            }

        // 1. Меняем возвращаемый тип: добавляем третьим параметром UserEntity?
        public async Task<(bool IsSuccess, List<ValidationResult> Messages, UserEntity? User)> CheckUserAsync(UserEntity userInput)
        {
            var validationResults = await ValidateInputAsync(userInput);
            var hasCriticalErrors = validationResults.Any(r => r.State == ValidationState.Error);

            UserEntity? authenticatedUser = null;

            // 2. Если ошибок нет — вытаскиваем тёпленького юзера из базы для логгера и UI
            if (!hasCriticalErrors)
            {
                // На строке 32 пиши вот так:
                if (!hasCriticalErrors && !string.IsNullOrWhiteSpace(userInput.Email))
                {
                    authenticatedUser = await _userService.GetByEmailAsync(userInput.Email);
                }

            }

            return (!hasCriticalErrors, validationResults, authenticatedUser);
        }


        private async Task<List<ValidationResult>> ValidateInputAsync(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // Ставим "!", гася панику компилятора по поводу возможного null 🤫
            var emailResult = await ValidateEmailAsync(input.Email!);
            results.Add(emailResult);

            var passwordResult = emailResult.State switch
            {
                ValidationState.Error => new ValidationResult
                {
                    Field = TypeField.PasswordType,
                    State = ValidationState.Info,
                    Message = "Текущий пароль"
                },
                // И здесь глушим предупреждение через "!"
                ValidationState.Success => await VerifyPasswordPlainTextAsync(input.Email!, input.Password!),
                _ => await VerifyPasswordPlainTextAsync(input.Email!, input.Password!)
            };

            results.Add(passwordResult);
            return results;
        }


        private async Task<ValidationResult> ValidateEmailAsync(string email)
            {
                var result = new ValidationResult { Field = TypeField.EmailType };

                try
                {
                    if (string.IsNullOrWhiteSpace(email))
                        return SetResult(result, ValidationState.Error, "Email не может быть пустым");

                    if (!_validateField.IsValidEmailFormat(email))
                        return SetResult(result, ValidationState.Error, "Некорректный формат email");

                    var emailExists = await _userService.CheckEmailExistsAsync(email);

                    return emailExists
                        ? SetResult(result, ValidationState.Success, "Email найден")
                        : SetResult(result, ValidationState.Error, "Email не найден");
                }
                catch (Exception ex)
                {
                    return SetResult(result, ValidationState.Error, $"Ошибка проверки email: {ex.Message}");
                }
            }

            private async Task<ValidationResult> VerifyPasswordPlainTextAsync(string email, string password)
            {
                var result = new ValidationResult { Field = TypeField.PasswordType };

                try
                {
                    if (string.IsNullOrWhiteSpace(password))
                        return SetResult(result, ValidationState.Error, "Пароль не может быть пустым");

                    var user = await _userService.GetByEmailAsync(email);
                    if (user == null)
                        return SetResult(result, ValidationState.Error, "Пользователь не найден");

                    return user.Password == password
                        ? SetResult(result, ValidationState.Success, "Пароль верен")
                        : SetResult(result, ValidationState.Error, "Неверный пароль");
                }
                catch (Exception ex)
                {
                    return SetResult(result, ValidationState.Error, $"Ошибка проверки пароля: {ex.Message}");
                }
            }

            // Вспомогательный хелпер для лаконичной мутации и возврата объекта результата
            private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
            {
                res.State = state;
                res.Message = message;
                return res;
            }
        }
    }




