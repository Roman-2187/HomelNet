using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Models;

namespace HomeNetCore.Interfaces
{
    public interface IMessageRepository
    {
        Task<bool> SaveMessageAsync(MessageEntity message);
        Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId);
        Task<bool> MarkAsReadAsync(int senderId, int receiverId);

        // 🔥 ТОТ САМЫЙ МЕТОД: Посчитать непрочитанные от конкретного собеседника текущему юзеру
        Task<int> GetUnreadCountAsync(int currentUserId, int senderId);

        Task<bool> RemoveMessageByIdAsync(int id);
        Task<bool> ClearChatHistoryAsync(int senderId, int receiverId);

    }
}
