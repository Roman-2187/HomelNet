using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Repositories
{
    public interface IMessageRepo
    {
        // ==========================================
        // 📥 КОМАНДЫ (Открыты для всех через new)
        // ==========================================
        public record SaveRequested(MessageEntity Message);
        public record UpdateRequested(MessageEntity Message);
        public record DeleteRequested(Guid MessageId);
        public record HistoryRequested(Guid SenderId, Guid ReceiverId);
        public record MarkAsReadRequested(Guid SenderId, Guid ReceiverId);
        public record UnreadCountRequested(Guid CurrentUserId, Guid SenderId);
        public record ClearChatRequested(Guid SenderId, Guid ReceiverId);

        // ==========================================
        // 📤 ФАКТЫ (Снаружи ТОЛЬКО для подписки / Только чтение)
        // ==========================================
        public interface ISaved { MessageEntity? Message { get; } string? MessageText { get; } }
        public interface IUpdated { MessageEntity? Message { get; } string? MessageText { get; } }
        public interface IDeleted { MessageEntity? Message { get; } string? MessageText { get; } }
        public interface IHistoryLoaded { Guid SenderId { get; } Guid ReceiverId { get; } IEnumerable<MessageEntity> History { get; } }
        public interface IMarkedAsRead { Guid SenderId { get; } Guid ReceiverId { get; } string? MessageText { get; } }
        public interface IUnreadCountCalculated { Guid CurrentUserId { get; } Guid SenderId { get; } int Count { get; } }
        public interface IChatCleared { Guid SenderId { get; } Guid ReceiverId { get; } string? MessageText { get; } }
    }
}
