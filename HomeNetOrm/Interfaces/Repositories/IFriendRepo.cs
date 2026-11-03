using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    public interface IFriendRepo
    {
        Task<bool> AddFriendAsync(FriendEntity friend);

        // ❌ Теперь удаляем связь по её уникальному Guid Id
        Task<bool> RemoveFriendByIdAsync(Guid id);

        // 👥 Выборка друзей по Guid пользователя
        Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(Guid userId);
    }
}
