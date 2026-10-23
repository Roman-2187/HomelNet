using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetOrm.Interfaces.Generators
{
    public interface IMessageSqlGenerator : ISqlGenerator<MessageEntity>
    {
        string GenerateSelectChatHistory();
        string GenerateMarkAsRead();
        string GenerateUnreadCount();
        string GenerateClearChatHistory();
    }
}
