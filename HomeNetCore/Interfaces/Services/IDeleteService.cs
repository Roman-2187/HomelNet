using HomeNetCore.Enums;
using HomeNetCore.Models.Entity;
using HomeNetCore.Models.Validation;

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



        public record DeleteVerdict(bool IsValid,List<ValidationResult> Results, Guid? ParsedId, IEnumerable<UserEntity>? UpdatedUsers = null)  
        {  
            public string HistoryMessage { get; set; } = string.Empty;
        }

    }
}
