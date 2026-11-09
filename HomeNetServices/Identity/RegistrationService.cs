using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNet.Network.Network.ApiClient;

namespace HomeNetServices.Identity
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserApiClient _apiClient;

        // 🔥 Память для последней проверки Email
        private string? _lastCheckedEmail;
        private ValidationResult? _lastEmailCheckResult;

        public RegistrationService(IUserApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IRegistrationService.Verdict> RegisterUserAsync(UserEntity user)
        {
            // 1. Запускаем базовую валидацию (проверка форматов всех полей)
            var validationResults = ValidateInput(user);

            // 2. 🔥 ПОДМЕШИВАЕМ РЕЗУЛЬТАТ ПРОВЕРКИ EMAIL ИЗ КЭША
            // Если мы уже проверяли этот же Email на потерю фокуса, берем результат оттуда
            if (_lastCheckedEmail == user.Email && _lastEmailCheckResult != null)
            {
                // Удаляем временный результат ("Формат корректен")
                validationResults.RemoveAll(r => r.Field == TypeField.EmailType);

                // Вставляем сохраненный результат ("Email свободен" или "Занят" или "Ошибка сети")
                validationResults.Add(_lastEmailCheckResult);
            }

            // 3. Проверяем, есть ли ошибки после всех проверок
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IRegistrationService.Verdict(false, validationResults, null);

            // 4. Если ошибок нет — отправляем на сервер
            bool success = await _apiClient.PushUserAsync(user);

            if (success)
                return new IRegistrationService.Verdict(true, validationResults, user);

            // 5. Если сервер отклонил регистрацию (например, email заняли, пока мы вводили пароль)
            var errorResult = new ValidationResult
            {
                Field = TypeField.EmailType,
                State = ValidationState.Error,
                Message = "Сервер отклонил регистрацию (возможно, Email уже занят)."
            };

            return new IRegistrationService.Verdict(false, [errorResult], null);
        }

        private List<ValidationResult> ValidateInput(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // 1. Имя
            var nameRes = ValidateUserName(input.FirstName);
            results.Add(nameRes);

            // 2. Email (только формат!)
            var emailRes = ValidateEmail(input.Email);
            results.Add(emailRes);

            // 🎯 Защитный барьер
            if (nameRes.State == ValidationState.Error || emailRes.State == ValidationState.Error)
            {
                results.Add(new ValidationResult { Field = TypeField.PasswordType, State = ValidationState.Info, Message = "Пароль должен содержать минимум 8 символов, буквы и цифры" });
                results.Add(new ValidationResult { Field = TypeField.ConfirmedPasswordType, State = ValidationState.Info, Message = "Пароли должны совпадать" });
                return results;
            }

            // 3. Пароли
            var passRes = ValidatePassword(input.Password);
            var confirmRes = ValidateConfirmedPassword(input.Password, input.ConfirmPassword);
            results.AddRange(new[] { passRes, confirmRes });

            return results;
        }


        public async Task<ValidationResult> CheckEmailOnBlurAsync(string email)
        {
            var res = new ValidationResult { Field = TypeField.EmailType };

            // 1. Проверяем формат
            var formatCheck = ValidateEmail(email);
            if (formatCheck.State == ValidationState.Error)
            {
                // Если формат сломался, сбрасываем память
                _lastCheckedEmail = null;
                _lastEmailCheckResult = null;
                return formatCheck;
            }

            // 2. Идем в API
            try
            {
                bool exists = await _apiClient.CheckEmailExistsAsync(email);

                var result = exists
                    ? SetResult(res, ValidationState.Error, "Этот Email уже занят")
                    : SetResult(res, ValidationState.Success, "Email свободен");

                // 🔥 Запоминаем результат
                _lastCheckedEmail = email;
                _lastEmailCheckResult = result;

                return result;
            }
            catch (Exception )
            {
                // Возвращаем ошибку и тоже запоминаем
                var errorResult = SetResult(res, ValidationState.Error, "Ошибка сети. Не удалось проверить Email.");

                _lastCheckedEmail = email;
                _lastEmailCheckResult = errorResult;

                return errorResult;
            }
        }

        private ValidationResult ValidateUserName(string? userName)
        {
            var res = new ValidationResult { Field = TypeField.NameType };

            if (string.IsNullOrWhiteSpace(userName))
                return SetResult(res, ValidationState.Error, "Имя пользователя не может быть пустым");

            if (!ValidationFormat.IsValidUserName(userName))
                return SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов");

            return SetResult(res, ValidationState.Success, "Имя пользователя принято");
        }

        private ValidationResult ValidateEmail(string? email)
        {
            var res = new ValidationResult { Field = TypeField.EmailType };

            if (string.IsNullOrWhiteSpace(email))
                return SetResult(res, ValidationState.Error, "Email не может быть пустым");

            if (!ValidationFormat.IsValidEmail(email))
                return SetResult(res, ValidationState.Error, "Некорректный формат email");

            return SetResult(res, ValidationState.Info, "Формат email корректен");
        }

        private ValidationResult ValidatePassword(string? password)
        {
            var res = new ValidationResult { Field = TypeField.PasswordType };

            if (string.IsNullOrWhiteSpace(password))
                return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            if (!ValidationFormat.IsValidPassword(password))
                return SetResult(res, ValidationState.Error, "Пароль должен содержать минимум 8 символов, буквы и цифры");

            return SetResult(res, ValidationState.Success, "Пароль принят");
        }

        private ValidationResult ValidateConfirmedPassword(string? password, string? confirmedPassword)
        {
            var res = new ValidationResult { Field = TypeField.ConfirmedPasswordType };

            if (string.IsNullOrWhiteSpace(confirmedPassword))
                return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            if (confirmedPassword != password)
                return SetResult(res, ValidationState.Error, "Пароли не совпадают");

            return SetResult(res, ValidationState.Success, "Пароли совпадают");
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state;
            res.Message = message;
            return res;
        }
    }
}