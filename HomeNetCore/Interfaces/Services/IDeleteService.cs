using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.Services
{
    public interface IDeleteService
    {
        Task<(bool IsSuccess, string Message)> DeleteUserAsync(int id);
        Task<(bool IsSuccess, string Message, UserEntity? User)> SearchUserAsync(string targetUserId);
    }
}