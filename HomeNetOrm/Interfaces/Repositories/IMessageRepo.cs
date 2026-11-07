using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    // 🔥 Наследуем плоский корневой IRepository
    public interface IMessageRepo : IRepository ,ICrudRepository<MessageEntity>
    {
       

        // ==========================================
        // 📤 ФАКТЫ 
        // ==========================================
        public record Saved(MessageEntity Message);
        public record Updated(MessageEntity Message);
        public record Deleted(Guid MessageId);
        public record ChatCleared(Guid SenderId, Guid ReceiverId);

        // ==========================================
        // 🔍 ПРЯМЫЕ МЕТОДЫ (Только уникальный домен чатов)
        // ==========================================
        
        Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId);
        Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId);
        Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId);
        Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId);
    }
}
