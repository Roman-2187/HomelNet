using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeNetCore.Interfaces.Services
{
    public interface IDeleteService
    {
        // 🔥 ОТКРЫВАЕМ ТЕЛО РЕКОРДА: Переносим метод WithResult СТРОГО внутрь самого SearchVerdict!
        public record SearchVerdict(bool IsValid, List<ValidationResult> Results, UserEntity? FoundUser)
        {
            public SearchVerdict WithResult(ValidationResult res, ValidationState state, string message)
            {
                res.Update(state, message); // Используем наш новый встроенный метод!
                return this; // Теперь 'this' — это именно SearchVerdict! Всё чётко! 👍
            }
        }

        Task<IEnumerable<UserEntity>> GetAllUsersAsync();

        Task<SearchVerdict> SearchUserAsync(string targetUserId);

        Task<DeleteVerdict> DeleteUserAsync(string targetUserId, UserEntity? selectedUser);

        // В файле IDeleteUserVm.cs у нас лежит Deleted рекорд, а тут DeleteVerdict
        public record DeleteVerdict(
     bool IsValid,
     List<ValidationResult> Results,
     int? ParsedId,
     IEnumerable<UserEntity>? UpdatedUsers = null)
        {
            // 🔥 Сюда сервис запишет уже готовый текст для ListBox-а истории
            public string HistoryMessage { get; set; } = string.Empty;
        }

    }
}
