using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IFriendRepository
    {
        Task<bool> AddFriendAsync(FriendEntity friend);
        Task<bool> RemoveFriendByIdAsync(int id); // Наш метод удаления! ❌
        Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(int userId);
    }
}

