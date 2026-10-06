using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IUserRepository
    {
        Task DeleteByIdAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
        Task<List<UserEntity>> GetAllAsync();
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByIdAsync(Guid id);
        Task<UserEntity> InsertUserAsync(UserEntity user);
        Task UpdateAsync(UserEntity user);
    }
}