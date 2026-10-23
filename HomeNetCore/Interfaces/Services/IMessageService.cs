using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IMessageService
    {
        // Вердикт по аналогии с IRegistrationService.Verdict
        public record ChatVerdict(bool IsValid, string ErrorMessage, MessageEntity? CreatedMessage);

        Task<ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text);

        /// <summary>
        /// Глобальный киберпанк-ивент шины: выстреливает каждый раз, 
        /// когда в СУБД сохраняется или прилетает новое сообщение.
        /// </summary>
        public record MessageReceived(MessageEntity Message);

        /// <summary>
        /// Вытаскивает историю зашифрованной переписки между двумя пользователями из СУБД.
        /// </summary>
        Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId);
    }
}