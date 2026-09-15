using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Messaging;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetOrm.Interfaces;

namespace HomeNetServices.Services.Identity
{

    public class RegisterService : IRegisterService
    {
        private readonly IUserService _userService;
        private readonly ValidationFormat _validateField = new();

        public RegisterService(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // Теперь метод возвращает наш четкий record VRegistrationVerdict!
        public async Task<RegistrationVerdict> RegisterUserAsync(UserEntity user)
        {
            // 1. Пошаговая валидация
            var validationResults = await ValidateInputAsync(user);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new RegistrationVerdict(false, validationResults, null);

            // 2. Чистое сохранение — объект уже готов, никакого маппинга! 💎
            try
            {
                await _userService.AddUserAsync(user);
                return new RegistrationVerdict(true, validationResults, user);
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult
                {
                    State = ValidationState.Error,
                    Message = $"Ошибка сохранения пользователя: {ex.Message}"
                };
                return new RegistrationVerdict(false, new List<ValidationResult> { errorResult }, null);
            }
        }

        private async Task<List<ValidationResult>> ValidateInputAsync(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // Вытаскиваем данные прямо из свойств реактивного UserEntity
            var nameRes = ValidateUserName(input.FirstName);
            var passRes = ValidatePassword(input.Password);
            var confirmRes = ValidateConfirmedPassword(input.Password, input.ConfirmPassword); // 🔥 Наш NotMapped параметр!

            results.AddRange(new[] { nameRes, passRes, confirmRes });

            var emailRes = new ValidationResult { Field = TypeField.EmailType };

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Email не может быть пустым");
            }
            else if (!_validateField.IsValidEmailFormat(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Некорректный формат email");
            }
            else
            {
                emailRes = results.Any(r => r.State == ValidationState.Error) switch
                {
                    true => SetResult(emailRes, ValidationState.Success, "Формат email корректен (ожидание отправки)"),
                    false => await ValidateEmailAsync(input.Email)
                };
            }

            results.Add(emailRes);
            return results;
        }

        // Остальные методы валидации строк (ValidateUserName, ValidatePassword и т.д.) остаются БЕЗ изменений...
        private ValidationResult ValidateUserName(string? userName)
        {
            var res = new ValidationResult { Field = TypeField.NameType };
            if (string.IsNullOrWhiteSpace(userName)) return 
                    SetResult(res, ValidationState.Error, "Имя пользователя не может быть пустым");
            return !_validateField.ValidateUserNameFormat(userName) ? 
                SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов") :
                SetResult(res, ValidationState.Success, "Имя пользователя принято");
        }

        private ValidationResult ValidatePassword(string? password)
        {
            var res = new ValidationResult { Field = TypeField.PasswordType };
            if (string.IsNullOrWhiteSpace(password)) return
                    SetResult(res, ValidationState.Error, "Пароль не может быть пустым");
            return !_validateField.ValidatePasswordFormat(password) ?
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

        private async Task<ValidationResult> ValidateEmailAsync(string email)
        {
            var res = new ValidationResult { Field = TypeField.EmailType };
            try { return await _userService.CheckEmailExistsAsync(email) ?
                    SetResult(res, ValidationState.Error, "Email уже зарегистрирован") :
                    SetResult(res, ValidationState.Success, "Email свободен и принят"); }
            catch (Exception ex) { return SetResult(res, ValidationState.Error, $"Ошибка проверки email: {ex.Message}"); }
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state; res.Message = message; return res;
        }
    }
}
