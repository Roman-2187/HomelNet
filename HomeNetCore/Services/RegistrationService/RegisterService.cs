using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.InputUserData;
using HomeNetCore.Services.UsersServices;


    namespace HomeNetCore.Services
    {
        public class RegisterService
        {
            private readonly UserService _userService;
            private readonly ValidationFormat _validateField = new();

            public RegisterService(UserService userService)
            {
                _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            }

            public async Task<(bool IsSuccess, List<ValidationResult> Messages, UserEntity? CreatedUser)> RegisterUserAsync(CreateUserInput userInput)
            {
                // 1. Пошаговая валидация
                var validationResults = await ValidateInputAsync(userInput);
                if (validationResults.Any(r => r.State == ValidationState.Error))
                    return (false, validationResults, null);

                // 2. Создание и сохранение модели
                try
                {
                    var user = CreateUserEntity(userInput);
                    await _userService.AddUserAsync(user);
                    return (true, validationResults, user);
                }
                catch (Exception ex)
                {
                    var errorResult = new ValidationResult
                    {
                        State = ValidationState.Error,
                        Message = $"Ошибка сохранения пользователя: {ex.Message}"
                    };
                    return (false, new List<ValidationResult> { errorResult }, null);
                }
            }

        private async Task<List<ValidationResult>> ValidateInputAsync(CreateUserInput input)
        {
            var results = new List<ValidationResult>();

            // ШАГ 1: Локальные быстрые проверки (Имя, Пароль, Подтверждение)
            var nameRes = ValidateUserName(input.UserName);
            var passRes = ValidatePassword(input.Password);
            var confirmRes = ValidateConfirmedPassword(input.Password, input.ConfirmPassword);

            results.AddRange(new[] { nameRes, passRes, confirmRes });

            // ШАГ 2: Честная проверка формата Email (Эсэмэски вернулись!) 🚀📧
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
                // ШАГ 3: Магия switch. В базу идём ТОЛЬКО если локальные поля и сам email идеальны! 🛸🔒
                emailRes = results.Any(r => r.State == ValidationState.Error) switch
                {
                    // Если где-то в форме есть косяк — базу по поводу уникальности не дёргаем,
                    // но формат-то у нас уже прошёл! Пишем, что имейл корректен.
                    true => SetResult(emailRes, ValidationState.Success, "Формат email корректен (ожидание отправки)"),

                    // Если вообще всё чисто — проверяем занятость в SQLite
                    false => await ValidateEmailAsync(input.Email)
                };
            }

            results.Add(emailRes);
            return results;
        }

        private UserEntity CreateUserEntity(CreateUserInput input) => new()
            {
                FirstName = input.UserName,
                Email = input.Email,
                Password = input.Password
            };

            private ValidationResult ValidateUserName(string userName)
            {
                var res = new ValidationResult { Field = TypeField.NameType };

                if (string.IsNullOrWhiteSpace(userName))
                    return SetResult(res, ValidationState.Error, "Имя пользователя не может быть пустым");

                return !_validateField.ValidateUserNameFormat(userName)
                    ? SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов")
                    : SetResult(res, ValidationState.Success, "Имя пользователя принято");
            }

            private ValidationResult ValidatePassword(string password)
            {
                var res = new ValidationResult { Field = TypeField.PasswordType };

                if (string.IsNullOrWhiteSpace(password))
                    return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

                return !_validateField.ValidatePasswordFormat(password)
                    ? SetResult(res, ValidationState.Error, "Пароль должен содержать минимум 8 символов, буквы и цифры")
                    : SetResult(res, ValidationState.Success, "Пароль принято");
            }

            private ValidationResult ValidateConfirmedPassword(string password, string confirmedPassword)
            {
                var res = new ValidationResult { Field = TypeField.ConfirmedPasswordType };

                if (string.IsNullOrWhiteSpace(confirmedPassword))
                    return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

                return confirmedPassword != password
                    ? SetResult(res, ValidationState.Error, "пароли не совпадают")
                    : SetResult(res, ValidationState.Success, "пароли совпадают");
            }

        private async Task<ValidationResult> ValidateEmailAsync(string email)
        {
            var res = new ValidationResult { Field = TypeField.EmailType };

            try
            {
                // Сюда прилетает только гарантированно правильный формат! Проверяем уникальность.
                return await _userService.CheckEmailExistsAsync(email)
                    ? SetResult(res, ValidationState.Error, "Email уже зарегистрирован")
                    : SetResult(res, ValidationState.Success, "Email свободен и принят");
            }
            catch (Exception ex)
            {
                return SetResult(res, ValidationState.Error, $"Ошибка проверки email: {ex.Message}");
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

