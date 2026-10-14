using Dapper;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Sinc;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий сообщений SiberNet.
    /// Работает строго на базе Guid для обеспечения распределенной репликации! 🦾
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly DbConnection _connection;
        private readonly IMessageSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus;

        public MessageRepository(IDbEngine dbEngine, IEventBus eventBus)
        {
            if (dbEngine == null) throw new ArgumentNullException(nameof(dbEngine));

            _connection = dbEngine.Connection;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Вытаскиваем специализированный генератор для сообщений
            _sqlGenerator = (IMessageSqlGenerator)dbEngine.GetGenerator<MessageEntity>();
        }

        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                // Заменяем ExecuteScalar на обычный Execute. База просто сохранит твой Guid.NewGuid()
                await _connection.ExecuteAsync(sql, message);

                // Ничего не перезаписываем! Оригинальный Guid остается в message.Id
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));

                return true;
            }

            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения сообщения через ОРМ: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ НА GUID: История чата
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            string sql = _sqlGenerator.GenerateSelectChatHistory();
            try
            {
                return await _connection.QueryAsync<MessageEntity>(sql, new { userId = senderId, friendId = receiverId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить историю чата через ОРМ: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ НА GUID: Статус прочтения
        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId)
        {
            string sql = _sqlGenerator.GenerateMarkAsRead();

            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления статуса прочтения сообщений через ОРМ: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ НА GUID: Подсчет непрочитанных
        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId)
        {
            string sql = _sqlGenerator.GenerateUnreadCount();

            try
            {
                return await _connection.ExecuteScalarAsync<int>(sql, new { CurrentUserId = currentUserId, SenderId = senderId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка подсчета непрочитанных через ОРМ: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ НА GUID: Удаление сообщения по его UUID
        public async Task<bool> RemoveMessageByIdAsync(Guid id)
        {
            var message = await _connection.QueryFirstOrDefaultAsync<MessageEntity>(
                _sqlGenerator.GenerateSelectById(), new { id = id });

            string sql = _sqlGenerator.GenerateDelete();
            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { id = id });

                if (rowsAffected > 0 && message != null)
                {
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageDeleted(message.Id));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления сообщения по ID через ОРМ: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ НА GUID: Полная очистка диалога
        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            string sql = _sqlGenerator.GenerateClearChatHistory();

            try
            {
                // Запросы возвращают UserEntity, где Id теперь гарантированно Guid
                var sender = await _connection.QueryFirstOrDefaultAsync<UserEntity>("SELECT id FROM users WHERE id = @id", new { id = senderId });
                var receiver = await _connection.QueryFirstOrDefaultAsync<UserEntity>("SELECT id FROM users WHERE id = @id", new { id = receiverId });

                int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });

                if (rowsAffected > 0 && sender != null && receiver != null)
                {
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.ChatCleared(sender.Id, receiver.Id));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при полной очистке чата через ОРМ: {ex.Message}");
            }
        }
    }
}
