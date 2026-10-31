using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.Repositories; // Контракт IUserRepo из Ядра
using HomeNetCore.Models.Entity;
using HomeNetCore.Models.Validation;
using HomeNetCore.Utils;
using HomeNet.Infrastructure.Shared.Interfaces.Routing; // Переехавший в инфраструктуру автобус

namespace HomeNetServices.Identity
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserRepo _userRepo;
        private readonly IEventBus _eventBus;

        public RegistrationService(IUserRepo userRepo, IEventBus eventBus)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Выполняет пошаговую валидацию и отправляет посылку на регистрацию в шину.
        /// </summary>
        public async Task<IRegistrationService.Verdict> RegisterUserAsync(UserEntity user)
        {
            // 1. Пошаговая доменная валидация полей ввода
            var validationResults = await ValidateInputAsync(user);
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IRegistrationService.Verdict(false, validationResults, null);

            // 2. ОТПРАВЛЯЕМ ПОСЫЛКУ: Команда улетает в шину. 
            // UserRepo поймает её, запишет в SQLite, а фоновый SyncCoordinator реплицирует в Postgres.
            await _eventBus.PublishAsync(this, new IUserRepo.InsertRequested(user));

            // Возвращаем вердикт успешного старта операции
            return new IRegistrationService.Verdict(true, validationResults, user);
        }

        private async Task<List<ValidationResult>> ValidateInputAsync(UserEntity input)
        {
            var results = new List<ValidationResult>();

            // ➡️ ШАГ 1: Валидируем Имя пользователя
            var nameRes = ValidateUserName(input.FirstName);
            results.Add(nameRes);

            // ➡️ ШАГ 2: Самостоятельная валидация Email
            var emailRes = new ValidationResult { Field = TypeField.EmailType };

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Email не может быть пустым");
                results.Add(emailRes);
            }
            else if (!ValidationFormat.IsValidEmail(input.Email))
            {
                emailRes = SetResult(emailRes, ValidationState.Error, "Некорректный формат email");
                results.Add(emailRes);
            }
            else
            {
                // 🔥 ЧЕСТНЫЙ И ПОНЯТНЫЙ ВЫЗОВ: Прямой метод чтения репозитория без инфраструктурного ада
                var isExist = await _userRepo.EmailExistsAsync(input.Email);

                emailRes = isExist
                    ? SetResult(emailRes, ValidationState.Error, "Email уже зарегистрирован")
                    : SetResult(emailRes, ValidationState.Success, "Email свободен и принят");
                results.Add(emailRes);
            }

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
            if (string.IsNullOrWhiteSpace(userName))
                return SetResult(res, ValidationState.Error, "Имя пользователя не может быть пустым");

            // Вызов через статический инструмент-алгоритм
            return !ValidationFormat.IsValidUserName(userName) ?
                SetResult(res, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов") :
                SetResult(res, ValidationState.Success, "Имя пользователя принято");
        }

        private ValidationResult ValidatePassword(string? password)
        {
            var res = new ValidationResult { Field = TypeField.PasswordType };
            if (string.IsNullOrWhiteSpace(password))
                return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            // Вызов через статический инструмент-алгоритм
            return !ValidationFormat.IsValidPassword(password) ?
                SetResult(res, ValidationState.Error, "Пароль должен содержать минимум 8 символов, буквы и цифры") :
                SetResult(res, ValidationState.Success, "Пароль принято");
        }

        private ValidationResult ValidateConfirmedPassword(string? password, string? confirmedPassword)
        {
            var res = new ValidationResult { Field = TypeField.ConfirmedPasswordType };
            if (string.IsNullOrWhiteSpace(confirmedPassword))
                return SetResult(res, ValidationState.Error, "Пароль не может быть пустым");

            return confirmedPassword != password ?
                SetResult(res, ValidationState.Error, "пароли не совпадают") :
                SetResult(res, ValidationState.Success, "пароли совпадают");
        }

        private ValidationResult SetResult(ValidationResult res, ValidationState state, string message)
        {
            res.State = state;
            res.Message = message;
            return res;
        }
    }
}
