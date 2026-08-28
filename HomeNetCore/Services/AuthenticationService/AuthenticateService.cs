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
                _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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

                // 1. Сначала всегда валидируем Email
                var emailResult = await ValidateEmailAsync(input.Email);
                results.Add(emailResult);

                // 2. Через switch-выражение управляем проверкой пароля на основе статуса Email
                var passwordResult = emailResult.State switch
                {
                    // Если с Email ошибка — до пароля не дотрагиваемся, возвращаем нейтральный Info
                    ValidationState.Error => new ValidationResult
                    {
                        Field = TypeField.PasswordType,
                        State = ValidationState.Info,
                        Message = "Текущий пароль"
                    },

                    // Если Email успешно найден — только тогда асинхронно дёргаем проверку пароля
                    ValidationState.Success => await VerifyPasswordPlainTextAsync(input.Email, input.Password),

                    // Дефолтный сценарий на случай других состояний
                    _ => await VerifyPasswordPlainTextAsync(input.Email, input.Password)
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

                    var user = await _userService.GetUserByEmailAsync(email);
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




