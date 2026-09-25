using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;
using HomeNetCore.Models.Validation; // Юзаем твои готовые модели валидации!

namespace HomeNetCore.Interfaces.Services
{
    public interface IDeleteService
    {
        // 🎯 Полноценные комплексные вердикты в стиле регистрации
        public record SearchVerdict(bool IsValid, List<ValidationResult> Results, UserEntity? FoundUser);
        

        Task<IEnumerable<UserEntity>> GetAllUsersAsync();
        Task<SearchVerdict> SearchUserAsync(string targetUserId);
        Task<DeleteVerdict> DeleteUserAsync(string targetUserId, UserEntity? selectedUser);

        // В файле IDeleteService.cs
        public record DeleteVerdict(
            bool IsValid,
            List<ValidationResult> Results,
            int? ParsedId,
            IEnumerable<UserEntity>? UpdatedUsers = null 
        );

    }
}
