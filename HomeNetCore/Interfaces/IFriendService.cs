using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public interface IFriendService
    {
        Task<bool> AddFriendToUserAsync(int userId, int friendId);
        Task<IEnumerable<UserEntity>> GetFriendsListAsync(int userId);
    }
}