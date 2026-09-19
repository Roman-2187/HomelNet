using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IUserService
    {
        // Только чистые базовые операции с базой/репозиторием
        Task InsertUserAsync(UserEntity user);
        Task DeleteByIdAsync(int userId);
        Task<List<UserEntity>> GetAllAsync();
        Task<UserEntity?> GetByIdAsync(int userId);
        Task<UserEntity?> GetByEmailAsync(string email);
    }
}
