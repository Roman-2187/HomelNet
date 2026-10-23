using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IFriendService
    {
        // 🤝 Контракт добавления контактов по Guid
        Task<bool> AddFriendToUserAsync(Guid userId, Guid friendId);

        // 👥 Контракт выгрузки списка друзей по Guid пользователя
        Task<IEnumerable<UserEntity>> GetFriendsListAsync(Guid userId);
    }
}
