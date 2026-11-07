using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    public interface IUserRepo : IRepository , ICrudRepository<UserEntity>
    {
       
        // ==========================================
        // 📤 ФАКТЫ 
        // ==========================================
        public record Inserted(UserEntity User, string? Message);
        public record Updated(UserEntity User, string? Message);
        public record Deleted(Guid UserId);

        // ==========================================
        // 🔍 ПРЯМЫЕ МЕТОДЫ
        // ==========================================
        Task<bool> EmailExistsAsync(string? email);
        Task<UserEntity?> GetByEmailAsync(string email);
      
    }
}