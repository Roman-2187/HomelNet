using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using System;

namespace HomeNetServices.Services.Identity
{
    /// <summary>
    /// Автономный диспетчер сообщений SiberNet.
    /// Зависит только от интерфейсов ядра, никаких ссылок на презентацию UI!
    /// </summary>
    public class MessageService : IDisposable,IMessageService
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageRepository _messageRepo;

        public MessageService(IEventBus eventBus, IMessageRepository messageRepo)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));

            // Ловим намерение отправить
            _eventBus.Subscribe<IChatViewModel.Send>(OnSendMessageRequested);
        }

        private async void OnSendMessageRequested(IChatViewModel.Send msg)
        {
            if (msg == null) return;

            // 🔥 ЧИСТОТА: ID отправителя прилетает прямо в рекорде из UI!
            var entity = new MessageEntity
            {
                SenderId = msg.SenderId,
                ReceiverId = msg.TargetId ?? 0,
                Text = msg.Text,
                MediaType = msg.FilePath != null ? "File" : "Text",
                FilePath = msg.FilePath,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _messageRepo.SaveMessageAsync(entity);

                _eventBus.Publish(this, new IChatViewModel.Received(
                    MessageId: entity.Id,
                    SenderId: entity.SenderId,
                    Text: entity.Text,
                    ChatType: msg.ChatType,
                    FilePath: msg.FilePath
                ));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[Сбой базы чата]: {ex.Message}"));
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IChatViewModel.Send>(OnSendMessageRequested);
        }
    }
}
