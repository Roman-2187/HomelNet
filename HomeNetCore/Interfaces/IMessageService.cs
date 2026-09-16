using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId);
        Task<bool> ReadChatMessagesAsync(int senderId, int receiverId);
        Task<bool> SendMessageAsync(MessageEntity message);
    }
}