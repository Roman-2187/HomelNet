using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using HomeNetCore.Utils;



namespace HomeNetServices.Services.Identity
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserService _userService;

        // 🔥 СТАТИКА: Убрали private readonly ValidationFormat _validateField = new(); 
        // Теперь дергаем методы напрямую через класс-инструмент из Utils!

        public RegistrationService(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Выполняет регистрацию. Возвращает лаконичный вложенный вердикт из интерфейса.
        /// </summary>
        public async Task<IRegistrationService.Verdict> RegisterUserAsync(UserEntity user)
        {
            // 1. Пошаговая валидация
            var validationResults = await ValidateInputAsync(user);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IRegistrationService.Verdict(false, validationResults, null);

            // 2. Чистое сохранение
            try
            {
                await _userService.AddUserSecureAsync(user);
                return new IRegistrationService.Verdict(true, validationResults, user);
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult
                {
                    State = ValidationState.Error,
                    Message = $"Ошибка сохранения пользователя: {ex.Message}"
                };
                return new IRegistrationService.Verdict(false, new List<ValidationResult> { errorResult }, null);
            }
        }

        private async Task<List<ValidationResult>> ValidateInputAsync(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // ➡️ ШАГ 1: Валидируем Имя пользователя
            var nameRes = ValidateUserName(input.FirstName);
            results.Add(nameRes);

            // ➡️ ШАГ 2: Самостоятельная валидация Email (больше не зависит от Имени!)
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
                // 🎯 ТЕПЕРЬ ЗАПРОС В СУБД ИДЕТ ВСЕГДА! Плевать, что там с именем.
                emailRes = await ValidateEmailAsync(input.Email);
            }
            results.Add(emailRes);

            // 🎯 ШАГ 3: Защитный барьер для паролей
            // Если Имя или Email содержат ошибки — пароли не трогаем, держим в режиме Info (бирюзовый неон)
            if (nameRes.State == ValidationState.Error || emailRes.State == ValidationState.Error)
            {
                results.Add(new ValidationResult { Field = TypeField.PasswordType, State = ValidationState.Info, Message = "Пароль должен содержать минимум 8 символов, буквы и цифры" });
                results.Add(new ValidationResult { Field = TypeField.ConfirmedPasswordType, State = ValidationState.Info, Message = "Пароли должны совпадать" });

                return results; // Выходим раньше времени, скрывая ошибки паролей
            }

            // ➡️ ШАГ 4: Сюда дойдем, только когда Имя и Email без косяков
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

            // 🔥 Вызов через статический инструмент-алгоритм
            return !ValidationFormat.IsValidUserName(userName) ?
                SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов") :
                SetResult(res, ValidationState.Success, "Имя пользователя принято");
        }

        private ValidationResult ValidatePassword(string? password)
        {
            var res = new ValidationResult { Field = TypeField.PasswordType };
            if (string.IsNullOrWhiteSpace(password)) return
                    SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            // 🔥 Вызов через статический инструмент-алгоритм
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

        private async Task<ValidationResult> ValidateEmailAsync(string email)
        {
            var res = new ValidationResult { Field = TypeField.EmailType };
            try
            {
                return await _userService.CheckEmailExistsAsync(email) ?
                    SetResult(res, ValidationState.Error, "Email уже зарегистрирован") :
                    SetResult(res, ValidationState.Success, "Email свободен и принят");
            }
            catch (Exception ex) { return SetResult(res, ValidationState.Error, $"Ошибка проверки email: {ex.Message}"); }
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state; res.Message = message; return res;
        }
    }
}
