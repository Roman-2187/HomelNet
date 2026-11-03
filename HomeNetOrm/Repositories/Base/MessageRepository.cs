using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    public class MessageRepository : BaseRepository<MessageEntity>, IMessageRepo
    {
        private readonly IMessageSqlGenerator _messageSqlGenerator;
        private readonly IEventBus _eventBus;

        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _messageSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();
        }

        private record MessageSavedFact(MessageEntity Message) : IMessageRepo.ISaved;
        private record ChatClearedFact(Guid SenderId, Guid ReceiverId) : IMessageRepo.IChatCleared;

        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.LocalCreatedAt = DateTimeOffset.UtcNow;
            message.ServerCreatedAt = DateTimeOffset.UtcNow;
            message.UpdatedAt = DateTimeOffset.UtcNow;
            message.IsSynced = 0;

            var cmd = _messageSqlGenerator.GenerateInsert();
            await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));

            _eventBus.Publish(this, new MessageSavedFact(message));
            return true;
        }

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            var cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                _eventBus.Publish(this, new ChatClearedFact(senderId, receiverId));
            }

            return rowsAffected > 0;
        }

        // Чисто уникальная логика мессенджера (история, маркеры прочтения, счетчики)
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId) =>
            await ExecuteAsync(db => db.QueryAsync<MessageEntity>(_messageSqlGenerator.GenerateSelectChatHistory(senderId, receiverId).Sql, new { SenderId = senderId, ReceiverId = receiverId }));

        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId) =>
            await ExecuteAsync(async db => await db.ExecuteAsync(_messageSqlGenerator.GenerateMarkAsRead(senderId, receiverId).Sql, new { SenderId = senderId, ReceiverId = receiverId }) > 0);

        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId) =>
            await ExecuteAsync(db => db.ExecuteScalarAsync<int>(_messageSqlGenerator.GenerateUnreadCount(currentUserId, senderId).Sql, new { CurrentUserId = currentUserId, SenderId = senderId }));

        private class NullEventProxy : IEventBus { }
    }
}
