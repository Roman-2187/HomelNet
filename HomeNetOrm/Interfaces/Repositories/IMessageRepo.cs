using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    // 🔥 Наследуем плоский корневой IRepository
    public interface IMessageRepo : IRepository
    {
        // ==========================================
        // 📥 КОМАНДЫ (Открытые рекорды для шины)
        // ==========================================
        public record SaveRequested(MessageEntity Message);
        public record UpdateRequested(MessageEntity Message);
        public record DeleteRequested(Guid MessageId);
        public record ClearChatRequested(Guid SenderId, Guid ReceiverId);

        // ==========================================
        // 📤 ФАКТЫ (Защищенные интерфейсы фактов)
        // ==========================================
        public record Saved(MessageEntity Message);
        public record Updated(MessageEntity Message);
        public record Deleted(Guid MessageId);
        public record ChatCleared(Guid SenderId, Guid ReceiverId);

        // ==========================================
        // 🔍 ПРЯМЫЕ МЕТОДЫ (Только уникальный домен чатов)
        // ==========================================
        Task<bool> SaveMessageAsync(MessageEntity message);
        Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId);
        Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId);
        Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId);
        Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId);
    }
}
