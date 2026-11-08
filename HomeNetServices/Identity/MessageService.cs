using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNet.Network.Network.ApiClient;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNetServices.Services.Identity
{
    public class MessageService : IMessageService
    {
        private readonly IMessageApiClient _apiClient;
        private readonly IMessageRepo _messageRepo; // Для чтения истории

        public MessageService(IMessageApiClient apiClient, IMessageRepo messageRepo)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
        }

        public async Task<IMessageService.ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new IMessageService.ChatVerdict(false, "Сообщение не может быть пустым", null);

            var entity = new MessageEntity
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text.Trim(),
                MediaType = "Text",
                IsRead = false,
                LocalCreatedAt = DateTime.UtcNow,
                IsSynced = 1 // Улетает на сервер
            };

            try
            {
                // 🔥 Отправляем через API (API опубликует факт, SyncService запишет в SQLite)
                bool success = await _apiClient.PushMessageAsync(entity);

                if (success)
                    return new IMessageService.ChatVerdict(true, string.Empty, entity);
                else
                    return new IMessageService.ChatVerdict(false, "Сервер недоступен. Сообщение не отправлено.", null);
            }
            catch (Exception ex)
            {
                return new IMessageService.ChatVerdict(false, $"Ошибка API: {ex.Message}", null);
            }
        }

        public async Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId)
        {
          
                var allMessages = await _messageRepo.GetChatHistoryAsync(userId, friendId);
                return allMessages.OrderBy(m => m.LocalCreatedAt);
            
           
        }
    }
}