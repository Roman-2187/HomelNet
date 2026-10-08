using HomeNetCore.Models;
using HomeNetOrm.Interfaces;

namespace HomeNetOrm.DBProviders.Interfaces
{
    public interface IMessageSqlGenerator : ISqlGenerator<MessageEntity>
    {
        string GenerateSelectChatHistory();
        string GenerateMarkAsRead();
        string GenerateUnreadCount();
        string GenerateClearChatHistory();
    }
}
