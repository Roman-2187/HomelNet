using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public interface IDeleteService
    {
        Task<(bool IsSuccess, string Message)> DeleteUserAsync(int id);
        Task<(bool IsSuccess, string Message, UserEntity? User)> SearchUserAsync(string targetUserId);
    }
}