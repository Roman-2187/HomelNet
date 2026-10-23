using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Repositories
{
    public interface IMessageRepository : IRepository<MessageEntity>
    {
        Task<bool> SaveMessageAsync(MessageEntity message);

        // 🎯 ТЕПЕРЬ НА GUID: Идеально для распределенной синхронизации
        Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId);

        Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId);

        // Посчитать непрочитанные от конкретного собеседника текущему юзеру
        Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId);

        // Идентификатор самого сообщения тоже переводим на Guid
        Task<bool> RemoveMessageByIdAsync(Guid id);

        Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId);  
    }
}
