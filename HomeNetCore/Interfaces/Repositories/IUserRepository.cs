using HomeNetCore.Models;
using HomeNetCore.Interfaces.Repositories;

namespace HomeNetCore.Interfaces
{
    // Наследуем IRepository<UserEntity>, убирая дубликаты GetAll, GetById и Update!
    public interface IUserRepository : IRepository<UserEntity>
    {
        Task DeleteByIdAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity> InsertUserAsync(UserEntity user);
        Task MarkAsSyncedAsync(Guid id);
    }
}
