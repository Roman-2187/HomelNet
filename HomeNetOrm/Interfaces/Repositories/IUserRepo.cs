using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    // 🔥 Наследуем плоский корневой IRepository
    public interface IUserRepo : IRepository
    {
        // ==========================================
        // 📥 КОМАНДЫ (Свободные рекорды для шины)
        // ==========================================
        public record InsertRequested(UserEntity User);
        public record UpdateRequested(UserEntity User);
        public record DeleteRequested(Guid UserId);

        // ==========================================
        // 📤 ФАКТЫ (Защищенные интерфейсы только для чтения)
        // ==========================================
        public interface IInserted { UserEntity User { get; } string? Message { get; } }
        public interface IUpdated { UserEntity User { get; } string? Message { get; } }
        public interface IDeleted { Guid UserId { get; } }

        // ==========================================
        // 🔍 ПРЯМЫЕ МЕТОДЫ (Только уникальный домен юзеров)
        // ==========================================
        Task<bool> EmailExistsAsync(string? email);
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity> InsertUserAsync(UserEntity user);
    }
}
