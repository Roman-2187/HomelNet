using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Validation;
using HomeNet.Network.Network.ApiClient; // 🔥

namespace HomeNetServices.Identity
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IUserApiClient _apiClient; // 🔥 Только API!

        public AuthenticateService(IUserApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IAuthenticateService.Verdict> CheckUserAsync(UserEntity userInput)
        {
            if (userInput == null) throw new ArgumentNullException(nameof(userInput));

            if (!string.IsNullOrWhiteSpace(userInput.Email))
                userInput.Email = userInput.Email.Replace(" ", "");

            // 1. Валидация (локальная)
            var validationResults = ValidateInput(userInput);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IAuthenticateService.Verdict(false, validationResults, null);

            // 2. Проверка на сервере (онлайн)
            var authenticatedUser = await _apiClient.AuthenticateAsync(userInput.Email!, userInput.Password!);

            if (authenticatedUser != null)
            {
                return new IAuthenticateService.Verdict(true, validationResults, authenticatedUser);
            }
            else
            {
                var errorResult = new ValidationResult
                {
                    Field = TypeField.PasswordType,
                    State = ValidationState.Error,
                    Message = "Неверный email или пароль (или сервер недоступен)."
                };
                return new IAuthenticateService.Verdict(false, new List<ValidationResult> { errorResult }, null);
            }
        }

        private List<ValidationResult> ValidateInput(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // Валидация Email (только формат, без запроса в БД)
            var emailResult = new ValidationResult { Field = TypeField.EmailType };
            if (string.IsNullOrWhiteSpace(input.Email) || !ValidationFormat.IsValidEmail(input.Email))
                emailResult = SetResult(emailResult, ValidationState.Error, "Некорректный формат email");
            else
                emailResult = SetResult(emailResult, ValidationState.Success, "Email принят");

            results.Add(emailResult);

            // Валидация пароля (только формат)
            var passResult = new ValidationResult { Field = TypeField.PasswordType };
            if (string.IsNullOrWhiteSpace(input.Password))
                passResult = SetResult(passResult, ValidationState.Error, "Пароль не может быть пустым");
            else
                passResult = SetResult(passResult, ValidationState.Success, "Пароль принят");

            results.Add(passResult);

            return results;
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state;
            res.Message = message;
            return res;
        }
    }
}