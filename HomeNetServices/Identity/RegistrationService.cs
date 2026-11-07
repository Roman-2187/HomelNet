using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNet.Network.Network.ApiClient; // 🔥 Только API

namespace HomeNetServices.Identity
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserApiClient _apiClient; // 🔥 Только API!

        public RegistrationService(IUserApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IRegistrationService.Verdict> RegisterUserAsync(UserEntity user)
        {
            // 1. Пошаговая валидация (формат полей, без запросов в БД)
            var validationResults = ValidateInput(user);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IRegistrationService.Verdict(false, validationResults, null);

            // 2. Отправка на сервер (сервер сам проверит уникальность Email)
            bool success = await _apiClient.PushUserAsync(user);

            if (success)
            {
                // Сервер подтвердил — координатор сохранит в SQLite.
                return new IRegistrationService.Verdict(true, validationResults, user);
            }
            else
            {
                var errorResult = new ValidationResult
                {
                    State = ValidationState.Error,
                    Message = "Сервер отклонил регистрацию (возможно, Email уже занят)."
                };
                return new IRegistrationService.Verdict(false, [errorResult], null);
            }
        }

        private List<ValidationResult> ValidateInput(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // ➡️ ШАГ 1: Валидируем Имя пользователя
            var nameRes = ValidateUserName(input.FirstName);
            results.Add(nameRes);

            // ➡️ ШАГ 2: Валидация Email (только формат, без запроса в БД)
            var emailRes = new ValidationResult { Field = TypeField.EmailType };

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Email не может быть пустым");
            }
            else if (!ValidationFormat.IsValidEmail(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Некорректный формат email");
            }
            else
            {
                // 🔥 Просто проверяем формат. Уникальность проверит сервер.
                emailRes = SetResult(emailRes, ValidationState.Success, "Email свободен и принят");
            }
            results.Add(emailRes);

            // 🎯 ШАГ 3: Защитный барьер для паролей
            if (nameRes.State == ValidationState.Error || emailRes.State == ValidationState.Error)
            {
                results.Add(new ValidationResult { Field = TypeField.PasswordType, State = ValidationState.Info, Message = "Пароль должен содержать минимум 8 символов, буквы и цифры" });
                results.Add(new ValidationResult { Field = TypeField.ConfirmedPasswordType, State = ValidationState.Info, Message = "Пароли должны совпадать" });

                return results;
            }

            // ➡️ ШАГ 4: Валидация паролей
            var passRes = ValidatePassword(input.Password);
            var confirmRes = ValidateConfirmedPassword(input.Password, input.ConfirmPassword);

            results.AddRange(new[] { passRes, confirmRes });
            return results;
        }

        private ValidationResult ValidateUserName(string? userName)
        {
            var res = new ValidationResult { Field = TypeField.NameType };
            if (string.IsNullOrWhiteSpace(userName)) return
                    SetResult(res, ValidationState.Error, "Имя пользователя не может быть пустым");

            return !ValidationFormat.IsValidUserName(userName) ?
                SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов") :
                SetResult(res, ValidationState.Success, "Имя пользователя принято");
        }

        private ValidationResult ValidatePassword(string? password)
        {
            var res = new ValidationResult { Field = TypeField.PasswordType };
            if (string.IsNullOrWhiteSpace(password)) return
                    SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            return !ValidationFormat.IsValidPassword(password) ?
                SetResult(res, ValidationState.Error, "Пароль должен содержать минимум 8 символов, буквы и цифры") :
                SetResult(res, ValidationState.Success, "Пароль принято");
        }

        private ValidationResult ValidateConfirmedPassword(string? password, string? confirmedPassword)
        {
            var res = new ValidationResult { Field = TypeField.ConfirmedPasswordType };
            if (string.IsNullOrWhiteSpace(confirmedPassword)) return
                    SetResult(res, ValidationState.Error, "Пароль не может быть пустым");
            return confirmedPassword != password ? SetResult(res, ValidationState.Error, "пароли не совпадают") :
                SetResult(res, ValidationState.Success, "пароли совпадают");
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state; res.Message = message; return res;
        }
    }
}