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
        public interface ISaved { MessageEntity Message { get; } }
        public interface IUpdated { MessageEntity Message { get; } }
        public interface IDeleted { Guid MessageId { get; } }
        public interface IChatCleared { Guid SenderId { get; } Guid ReceiverId { get; } }

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
