using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces
{
    public interface IUserService
    {
        // Базовые операции с репозиторием
        Task InsertUserAsync(UserEntity user);

        // 🔥 НАШ ПОТЕРЯШКА: Метод обновления сущности пользователя в СУБД
        Task UpdateUserAsync(UserEntity user);

        Task DeleteByIdAsync(Guid userId);

        Task<List<UserEntity>> GetAllAsync();

        Task<UserEntity?> GetByIdAsync(Guid userId);

        Task<UserEntity?> GetByEmailAsync(string email);

        Task<bool> CheckEmailExistsAsync(string email);
    }
}

