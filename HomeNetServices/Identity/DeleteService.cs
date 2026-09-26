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

        public DeleteService(ILogger logger, IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            return await _userService.GetAllAsync() ?? Enumerable.Empty<UserEntity>();
        }

        /// <summary>
        /// 🎯 Конвейер поиска пользователя (в твоём фирменном стиле вердиктов)
        /// </summary>
        public async Task<IDeleteService.SearchVerdict> SearchUserAsync(string targetUserId)
        {
            var validationResults = ValidateIdInput(targetUserId);

            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IDeleteService.SearchVerdict(false, validationResults, null);

            int id = int.Parse(targetUserId);
            try
            {
                var user = await _userService.GetByIdAsync(id);
                var res = validationResults.First();

                if (user != null)
                {
                    return new IDeleteService.SearchVerdict(true, validationResults, user)
                        .WithResult(res, ValidationState.Success, $"Найден: {user.FirstName} {user.LastName} ({user.Email})");
                }

                return new IDeleteService.SearchVerdict(false, validationResults, null)
                    .WithResult(res, ValidationState.Error, $"Пользователь с ID {id} не существует.");
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
            var idRes = new ValidationResult { Field = TypeField.IdType, State = ValidationState.Success }; // Добавили тип поля для UI
            validationResults.Add(idRes);

            if (id <= 0)
            {
                idRes.Update(ValidationState.Error, "Ошибка: Некорректный ID пользователя!");
                return new IDeleteService.DeleteVerdict(false, validationResults, null, null);
            }

            try
            {
                await _userService.DeleteByIdAsync(id);

                var allUsers = await _userService.GetAllAsync() ?? Enumerable.Empty<UserEntity>();
                _logger.LogDebug($"В системе {allUsers.Count()} пользователей");

                idRes.Update(ValidationState.Success, $"Пользователь с ID {id} успешно удален.");
                return new IDeleteService.DeleteVerdict(true, validationResults, id, allUsers);
            }
            catch (Exception ex)
            {
                idRes.Update(ValidationState.Error, $"Ошибка удаления из БД: {ex.Message}");
                return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
            }
        }

        private List<ValidationResult> ValidateIdInput(string targetUserId)
        {
            var res = new ValidationResult { Field = TypeField.IdType };

            if (string.IsNullOrWhiteSpace(targetUserId))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID не может быть пустым!") };

            if (!int.TryParse(targetUserId, out _))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID должен состоять только из цифр!") };

            return new List<ValidationResult> { res.Update(ValidationState.Success, "ID корректен для поиска") };
        }
    }

  
}
