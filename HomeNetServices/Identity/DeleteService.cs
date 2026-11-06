using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions; // 🔥 ПОДКЛЮЧАЕМ НАШИ ГЛОБАЛЬНЫЙ CRUD ОБВЕС
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetServices.Services.Identity
{
    public class DeleteService : IDeleteService
    {
        private readonly IUserRepo _userRepository;
        private readonly ICastomLogger _logger;

        public DeleteService(IUserRepo userRepo, ICastomLogger logger)
        {
            _userRepository = userRepo;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            // 🔥 ИСПРАВЛЕНО: Явно указали тип сущности <UserEntity>
            return await _userRepository.GetAllAsync() ?? Enumerable.Empty<UserEntity>();
        }

        /// <summary>
        /// 🎯 Конвейер поиска пользователя (в твоём фирменном стиле вердиктов)
        /// </summary>
        public async Task<IDeleteService.SearchVerdict> SearchUserAsync(string targetUserId)
        {
            var validationResults = ValidateIdInput(targetUserId);

            if (validationResults.Any(r => r.State == ValidationState.Error))
                return new IDeleteService.SearchVerdict(false, validationResults, null);

            Guid id = Guid.Parse(targetUserId);
            try
            {
                // 🔥 ИСПРАВЛЕНО: Добавлен дженерик-параметр <UserEntity> для точечного поиска по ID
                var user = await _userRepository.GetByIdAsync(id);
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

               
                await _userRepository.DeleteByIdAsync(id);

                // 🔥 ИСПРАВЛЕНО: Вызываем GetAllAsync с указанием <UserEntity>
                var allUsers = await _userRepository.GetAllAsync() ?? Enumerable.Empty<UserEntity>();
                _logger.LogDebug($"В системе {allUsers.Count()} пользователей");

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
                idRes.Update(ValidationState.Error, $"Ошибка удаления из БД: {ex.Message}");
                return new IDeleteService.DeleteVerdict(false, validationResults, id, null);
            }
        }

        private List<ValidationResult> ValidateIdInput(string targetUserId)
        {
            var res = new ValidationResult { Field = TypeField.IdType };

            if (string.IsNullOrWhiteSpace(targetUserId))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID не может быть пустым!") };

            // 🔥 ИСПРАВЛЕНО С int НА Guid: Теперь валидация проверяет валидный шестнадцатеричный Guid, а не цифры!
            if (!Guid.TryParse(targetUserId, out _))
                return new List<ValidationResult> { res.Update(ValidationState.Error, "Ошибка: ID должен быть валидным Guid (формат: 00000000-0000-0000-0000-000000000000)!") };

            return new List<ValidationResult> { res.Update(ValidationState.Success, "ID корректен для поиска") };
        }
    }
}
