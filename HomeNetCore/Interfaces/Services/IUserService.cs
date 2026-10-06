using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IUserService
    {
        // Только чистые базовые операции с базой/репозиторием
        Task InsertUserAsync(UserEntity user);

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        Task DeleteByIdAsync(Guid userId);

        Task<List<UserEntity>> GetAllAsync();

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        Task<UserEntity?> GetByIdAsync(Guid userId);

        Task<UserEntity?> GetByEmailAsync(string email);
    }
}
