using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    public interface IUserRepo : IRepository , ICrudRepository<UserEntity>
    {
        // ==========================================
        // 📥 КОМАНДЫ (Свободные рекорды для шины)
        // ==========================================
        public record InsertRequested(UserEntity User);
        public record UpdateRequested(UserEntity User);
        public record DeleteRequested(Guid UserId);

        // ==========================================
        // 📤 ФАКТЫ (🔥 Теперь это рекорды, а не интерфейсы!)
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