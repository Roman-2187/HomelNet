using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Repositories
{
    public interface IFriendRepository
    {
        Task<bool> AddFriendAsync(FriendEntity friend);

        // ❌ Теперь удаляем связь по её уникальному Guid Id
        Task<bool> RemoveFriendByIdAsync(Guid id);

        // 👥 Выборка друзей по Guid пользователя
        Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(Guid userId);
    }
}
