using System;
using System.Threading.Tasks;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Repositories
{
    public interface IUserRepo
    {
        // ==========================================
        // 📥 КОМАНДЫ ДЛЯ ШИНЫ (Запись / Изменение)
        // ==========================================
        public record InsertRequested(UserEntity User);
        public record UpdateRequested(UserEntity User);
        public record DeleteRequested(Guid UserId);

        // ==========================================
        // 📤 ФАКТЫ ДЛЯ ШИНЫ (События только для чтения)
        // ==========================================
        public interface IInserted { UserEntity? User { get; } string? Message { get; } }
        public interface IUpdated { UserEntity? User { get; } string? Message { get; } }
        public interface IDeleted { UserEntity User { get; } }

        // ==========================================
        // 🔍 ПРЯМЫЕ МЕТОДЫ ЧТЕНИЯ (Без ада с шиной)
        // ==========================================
        Task<bool> EmailExistsAsync(string? email);
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByIdAsync(Guid id);
    }
}
