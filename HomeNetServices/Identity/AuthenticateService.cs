using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetCore.Utils; 



namespace HomeNetServices.Identity
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IUserService _userService;

        // 🔥 СТАТИКА: Больше не плодим _validateField = new() через DI или приватные поля!

        public AuthenticateService(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Выполняет проверку пользователя. Возвращает лаконичный вложенный вердикт из интерфейса Ядра.
        /// </summary>
        public async Task<IAuthenticateService.Verdict> CheckUserAsync(UserEntity userInput)
        {
            var validationResults = await ValidateInputAsync(userInput);
            var hasCriticalErrors = validationResults.Any(r => r.State == ValidationState.Error);

            UserEntity? authenticatedUser = null;

            // Если критических ошибок нет — вытаскиваем тёпленького юзера из базы для логгера и UI
            if (!hasCriticalErrors && !string.IsNullOrWhiteSpace(userInput.Email))
            {
                authenticatedUser = await _userService.GetByEmailAsync(userInput.Email);
            }

            // 🔥 Возвращаем наш красивый вложенный рекорд
            return new IAuthenticateService.Verdict(!hasCriticalErrors, validationResults, authenticatedUser);
        }

        private async Task<List<ValidationResult>> ValidateInputAsync(UserEntity input)
        {
            var results = new List<ValidationResult>();

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

                // 🔥 Вызов через статический инструмент-алгоритм из Utils
                if (!ValidationFormat.IsValidEmail(email))
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

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state;
            res.Message = message;
            return res;
        }
    }
}

