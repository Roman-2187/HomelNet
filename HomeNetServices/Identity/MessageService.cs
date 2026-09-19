using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger _logger;

        public MessageService(IMessageRepository messageRepository, ILogger logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 💬 Отправить СМС или файл родственнику
        public async Task<bool> SendMessageAsync(MessageEntity message)
        {
            try
            {
                if (message == null) return false;

                bool success = await _messageRepository.SaveMessageAsync(message);
                if (success)
                {
                    _logger.LogInformation($"[Чат] Сообщение от ID {message.SenderId} отправлено пользователю ID {message.ReceiverId}.");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Чата] Не удалось сохранить сообщение: {ex.Message}");
                return false;
            }
        }

        // 🔍 Загрузить историю переписки диалога
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            try
            {
                var history = await _messageRepository.GetChatHistoryAsync(senderId, receiverId);
                _logger.LogInformation($"[Чат] Успешно загружена история диалога между ID {senderId} и ID {receiverId}.");
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Чата] Не удалось загрузить историю переписки: {ex.Message}");
                return new List<MessageEntity>();
            }
        }

        // 🧼 Прочитать сообщения
        public async Task<bool> ReadChatMessagesAsync(int senderId, int receiverId)
        {
            try
            {
                return await _messageRepository.MarkAsReadAsync(senderId, receiverId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Чата] Ошибка смены статуса прочтения: {ex.Message}");
                return false;
            }
        }
    }
}

