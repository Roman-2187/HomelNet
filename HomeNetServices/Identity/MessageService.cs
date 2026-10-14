using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;
using HomeNetOrm.Sinc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                IsSynced = 0
            };

            try
            {
                // Репозиторий сам сохранит запись и сам сделает ЕДИНСТВЕННЫЙ правильный выстрел в шину
                await _messageRepo.SaveMessageAsync(entity);

                return new IMessageService.ChatVerdict(true, string.Empty, entity);
            }

            catch (Exception ex)
            {
                return new IMessageService.ChatVerdict(false, $"Ошибка бэкенда при сохранении: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 🎯 УДОВЛЕТВОРЯЕМ КОНТРАКТ: Вытягиваем историю сообщений
        /// </summary>
        public async Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId)
        {
            try
            {
                // Запрашиваем из репозитория всю переписку (где я отправитель, а он получатель, И НАОБОРОТ)
                var allMessages = await _messageRepo.GetChatHistoryAsync(userId, friendId);


                // На всякий случай сортируем по времени, чтобы переписка шла сверху вниз
                return allMessages.OrderBy(m => m.CreatedAt);
            }
            catch (Exception)
            {
                // Если истории еще нет или репозиторий пуст — отдаем пустой список, чтобы не вешать UI
                return Array.Empty<MessageEntity>();
            }
        }
    }
}
