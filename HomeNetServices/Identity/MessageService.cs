using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;
using HomeNetOrm.Sinc;

namespace HomeNetServices.Services.Identity
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepo;
        private readonly IEventBus _eventBus;

        public MessageService(IMessageRepository messageRepo, IEventBus eventBus)
        {
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<IMessageService.ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text)
        {
            // Валидация прямо как в сервисе регистрации!
            if (string.IsNullOrWhiteSpace(text))
            {
                return new IMessageService.ChatVerdict(false, "Сообщение не может быть пустым", null);
            }

            var entity = new MessageEntity
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text.Trim(),
                MediaType = "Text",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                IsSynced = 0 // Наш оффлайн-флаг для координатора
            };

            try
            {
                // 1. Железобетонно пишем в локальную SQLite
                await _messageRepo.SaveMessageAsync(entity);

                // 2. Выстреливаем в автобус "вдогонку" — SiberNetSyncCoordinator поймает и утащит в Postgres!
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(entity));

                return new IMessageService.ChatVerdict(true, string.Empty, entity);
            }
            catch (Exception ex)
            {
                return new IMessageService.ChatVerdict(false, $"Ошибка бэкенда при сохранении: {ex.Message}", null);
            }
        }
    }
}
