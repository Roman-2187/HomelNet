using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNet.Network.Network.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetServices.Services.Identity
{
    public class DeleteService : IDeleteService
    {
        private readonly IUserApiClient _apiClient;
        private readonly ICastomLogger _logger;

        public DeleteService(IUserApiClient apiClient, ICastomLogger logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            // 🔥 Выгружаем юзеров с сервера (API)
            var users = await _apiClient.FetchFreshUsersAsync(Guid.Empty, DateTimeOffset.MinValue);
            return users ?? Enumerable.Empty<UserEntity>();
        }

        public async Task<IDeleteService.SearchVerdict> SearchUserAsync(string targetUserId)
        {
            var validationResults = ValidateIdInput(targetUserId);

            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IDeleteService.SearchVerdict(false, validationResults, null);

            Guid id = Guid.Parse(targetUserId);
            try
            {
                // 🔥 Ищем на сервере (API)
                var user = await _apiClient.GetUserByIdAsync(id);
                var res = validationResults.First();

                if (user != null)
                {
                    return new IDeleteService.SearchVerdict(true, validationResults, user)
                        .WithResult(res, ValidationState.Success, $"Найден: {user.FirstName} {user.LastName} ({user.Email})");
                }

                return new IDeleteService.SearchVerdict(false, validationResults, null)
                    .WithResult(res, ValidationState.Error, $"Пользователь с ID {id} не существует на сервере.");
            }
            catch (Exception ex)
            {
                var errorResult = new ValidationResult { State = ValidationState.Error, Message = $"Ошибка сети: {ex.Message}" };
                return new IDeleteService.SearchVerdict(false, new List<ValidationResult> { errorResult }, null);
            }
        }

        public async Task<IDeleteService.DeleteVerdict> DeleteUserAsync(string targetUserId, UserEntity? selectedUser)
        {
            Guid id = selectedUser?.Id ?? (Guid.TryParse(targetUserId, out Guid parsedId) ? parsedId : Guid.Empty);

            var validationResults = new List<ValidationResult>();
            var idRes = new ValidationResult { Field = TypeField.IdType, State = ValidationState.Success };
            validationResults.Add(idRes);

            if (id == Guid.Empty)
            {
                idRes.Update(ValidationState.Error, "Ошибка: Некорректный ID пользователя!");
                return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
            }

            try
            {
                string userInfo = selectedUser != null
                    ? $"{selectedUser.FirstName} {selectedUser.LastName} (ID {id})"
                    : $"ID {id} (Точечно)";

                // 🔥 Удаляем на сервере (API)
                bool success = await _apiClient.DeleteUserAsync(id);

                if (!success)
                {
                    idRes.Update(ValidationState.Error, "Сервер недоступен или удаление отклонено.");
                    return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
                }

                // 🔥 После удаления запрашиваем актуальный список с сервера
                var allUsers = await _apiClient.FetchFreshUsersAsync(Guid.Empty, DateTimeOffset.MinValue);
                _logger.LogDebug($"В системе {allUsers.Count()} пользователей (после удаления)");

                idRes.Update(ValidationState.Success, $"Пользователь с ID {id} успешно удален.");

                string readyHistoryLine = $"[{DateTime.Now:HH:mm:ss}] ❌ Удален: {userInfo}";

                var verdict = new IDeleteService.DeleteVerdict(true, validationResults, id, allUsers)
                {
                    HistoryMessage = readyHistoryLine
                };

                return verdict;
            }
            catch (Exception ex)
            {
                idRes.Update(ValidationState.Error, $"Ошибка удаления: {ex.Message}");
                return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
            }
        }

        private List<ValidationResult> ValidateIdInput(string targetUserId)
        {
            var res = new ValidationResult { Field = TypeField.IdType };

            if (string.IsNullOrWhiteSpace(targetUserId))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID не может быть пустым!") };

            if (!Guid.TryParse(targetUserId, out _))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID должен быть валидным Guid!") };

            return new List<ValidationResult> { res.Update(ValidationState.Success, "ID корректен для поиска") };
        }
    }
}