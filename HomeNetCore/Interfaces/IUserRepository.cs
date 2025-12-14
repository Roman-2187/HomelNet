using HomeNetCore.Models;

namespace HomeNetCore.Data.Interfaces
{
    public interface IUserRepository
    {
        Task DeleteByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<List<UserEntity>> GetAllAsync();
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByIdAsync(int id);
        Task<UserEntity> InsertUserAsync(UserEntity user);
        Task UpdateAsync(UserEntity user);
    }
}