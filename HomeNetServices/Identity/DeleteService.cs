using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;



namespace HomeNetServices.Services.Identity
{
    public class DeleteService : IDeleteService
    {
        private readonly IUserService _userService;
        private readonly ILogger _logger;

        public DeleteService(ILogger iloger, IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = iloger ?? throw new ArgumentNullException(nameof(iloger));
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            var users = await _userService.GetAllAsync();
            return users ?? Enumerable.Empty<UserEntity>();
        }

        /// <summary>
        /// 🎯 Конвейер поиска пользователя
        /// </summary>
        public async Task<IDeleteService.SearchVerdict> SearchUserAsync(string targetUserId)
        {
            var validationResults = ValidateIdInput(targetUserId);

            // Если на этапе парсинга строки получили ошибку — сразу выходим
            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IDeleteService.SearchVerdict(false, validationResults, null);

            int id = int.Parse(targetUserId);
            try
            {
                var user = await _userService.GetByIdAsync(id);
                var res = validationResults.First(); // Берем наш ValidationResult для поля ID

                if (user != null)
                {
                    SetResult(res, ValidationState.Success, $"Найден: {user.FirstName} {user.LastName} ({user.Email})");
                    return new IDeleteService.SearchVerdict(true, validationResults, user);
                }

                SetResult(res, ValidationState.Error, $"Пользователь с ID {id} не существует.");
                return new IDeleteService.SearchVerdict(false, validationResults, null);
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult { State = ValidationState.Error, Message = $"Ошибка БД: {ex.Message}" };
                return new IDeleteService.SearchVerdict(false, new List<ValidationResult> { errorResult }, null);
            }
        }

        /// <summary>
        /// 🎯 Конвейер удаления пользователя
        /// </summary>
        public async Task<IDeleteService.DeleteVerdict> DeleteUserAsync(string targetUserId, UserEntity? selectedUser)
        {
            int id = selectedUser?.Id ?? (int.TryParse(targetUserId, out int parsedId) ? parsedId : -1);

            var validationResults = new List<ValidationResult>();
            var idRes = new ValidationResult { State = ValidationState.Success };
            validationResults.Add(idRes);

            if (id <= 0)
            {
                SetResult(idRes, ValidationState.Error, "Ошибка: Некорректный ID пользователя!");
                return new IDeleteService.DeleteVerdict(false, validationResults, null, null);
            }

            try
            {
                await _userService.DeleteByIdAsync(id);

                // 🧠 Сходили ОДИН раз, залогировали и сохранили в переменную
                var allUsers = await _userService.GetAllAsync() ?? Enumerable.Empty<UserEntity>();
                _logger.LogDebug($"В системе {allUsers.Count()} пользователей");

                SetResult(idRes, ValidationState.Success, $"Пользователь с ID {id} успешно удален.");

                // Отдаем этот список тепленьким прямо во вью-модель 🚀
                return new IDeleteService.DeleteVerdict(true, validationResults, id, allUsers);
            }
            catch (Exception ex)
            {
                SetResult(idRes, ValidationState.Error, $"Ошибка удаления из БД: {ex.Message}");
                return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
            }
        }


        // Выделенная атомарная валидация строки ID перед запросом к базе
        private List<ValidationResult> ValidateIdInput(string targetUserId)
        {
            var results = new List<ValidationResult>();
            var res = new ValidationResult(); // Сюда можно прописать Field = TypeField.IdType, если добавишь в энум

            if (string.IsNullOrWhiteSpace(targetUserId))
            {
                results.Add(SetResult(res, ValidationState.Error, "Ошибка: ID не может быть пустым!"));
                return results;
            }

            if (!int.TryParse(targetUserId, out _))
            {
                results.Add(SetResult(res, ValidationState.Error, "Ошибка: ID должен состоять только из цифр!"));
                return results;
            }

            results.Add(SetResult(res, ValidationState.Success, "ID корректен для поиска"));
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
