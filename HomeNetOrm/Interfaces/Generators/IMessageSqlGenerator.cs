using HomeNetCore.Models.Entity;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    internal interface IMessageSqlGenerator : ISqlGenerator<MessageEntity>
    {
        SqlQueryCommand GenerateSelectChatHistory(Guid senderId, Guid receiverId);
        SqlQueryCommand GenerateMarkAsRead(Guid senderId, Guid receiverId);
        SqlQueryCommand GenerateUnreadCount(Guid currentUserId, Guid friendId);
        SqlQueryCommand GenerateClearChatHistory(Guid senderId, Guid receiverId);
    }
}
