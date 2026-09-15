using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IUserService
    {
        Task AddUserAsync(UserEntity user);
        Task<bool> CheckEmailExistsAsync(string? email);
        Task DeleteUserAsync(int userId, string? userName = null);
        Task<UserEntity?> FindUserByEmailAsync(string email);
        Task<List<UserEntity>> GetAllUsersAsync();
        Task<UserEntity?> GetUserByEmailAsync(string userEmail);
        Task<UserEntity?> GetUserByIdAsync(int userId);
    }
}