using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.Services
{
    public interface IMessageService
    {
        // Вердикт по аналогии с IRegistrationService.Verdict
        public record ChatVerdict(bool IsValid, string ErrorMessage, MessageEntity? CreatedMessage);

        Task<ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text);
    }
}