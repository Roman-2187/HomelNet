using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.Services
{
    public interface IFriendService
    {
        Task<bool> AddFriendToUserAsync(int userId, int friendId);
        Task<IEnumerable<UserEntity>> GetFriendsListAsync(int userId);
    }
}