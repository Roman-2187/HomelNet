using Dapper;
using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Exeptions; // Тут лежат DuplicateEmailException, DatabaseException и т.д.
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.DbInfrastructure; // Для IDbExceptionHandler
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    public class MessageRepository : BaseRepository<MessageEntity>, IMessageRepo
    {
        private readonly IMessageSqlGenerator _messageSqlGenerator;
        private readonly IEventBus? _eventBus;
        private readonly IDbExceptionHandler _errorHandler;

        // 🖥️ СЕРВЕРНЫЙ КОНСТРУКТОР (без шины)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ICastomLogger logger, IDbExceptionHandler errorHandler)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _messageSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        // 📱 КЛИЕНТСКИЙ КОНСТРУКТОР (с шиной)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ICastomLogger logger, IEventBus eventBus, IDbExceptionHandler errorHandler)
            : this(connectionFactory, sqlGenerator, logger, errorHandler)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<MessageEntity> InsertAsync(MessageEntity message)
        {
            if (message.Id == Guid.Empty) message.Id = Guid.NewGuid();

            var cmd = _messageSqlGenerator.GenerateInsert();

            try
            {
                // Просто выполняем запрос без статического перехватчика
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));

                Logger.LogInfo($"[MessageRepository] Сообщение '{message.Id}' успешно записано.");
                _eventBus?.Publish(this, new IMessageRepo.Saved(message));
                return message;
            }
            catch (Exception ex) when (_errorHandler.IsDuplicateKeyException(ex))
            {
                // Если SQLite выкинул UNIQUE constraint — кидаем доменное исключение
                throw new DuplicateMessageException(message.Id);
            }
            catch (Exception ex)
            {
                // Любая другая ошибка БД
                throw _errorHandler.Translate(ex);
            }
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var cmd = _messageSqlGenerator.GenerateUpdate();

            int rowsAffected = await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity)));

            if (rowsAffected > 0)
            {
                _eventBus?.Publish(this, new IMessageRepo.Updated(entity));
            }
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            var cmd = _messageSqlGenerator.GenerateDelete(id);

            int rowsAffected = await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params)));

            if (rowsAffected > 0)
            {
                _eventBus?.Publish(this, new IMessageRepo.Deleted(id));
            }
        }

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _messageSqlGenerator.GenerateSelectById(id);

            return await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.QueryFirstOrDefaultAsync<MessageEntity>(cmd.Sql, cmd.Params)));
        }

        public async Task<IEnumerable<MessageEntity>> GetAllAsync()
        {
            var cmd = _messageSqlGenerator.GenerateSelectAll();

            return await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params)));
        }

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            var cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);

            int rowsAffected = await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params)));

            if (rowsAffected > 0)
            {
                _eventBus?.Publish(this, new IMessageRepo.ChatCleared(senderId, receiverId));
            }

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId) =>
     await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
         ExecuteAsync(db => db.QueryAsync<MessageEntity>(
             _messageSqlGenerator.GenerateSelectChatHistory(senderId, receiverId).Sql,
             new
             {
                 SenderId1 = senderId,
                 ReceiverId1 = receiverId,
                 SenderId2 = receiverId,
                 ReceiverId2 = senderId
             })));
        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId) =>
            await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(async db => await db.ExecuteAsync(
                    _messageSqlGenerator.GenerateMarkAsRead(senderId, receiverId).Sql,
                    new { SenderId = senderId, ReceiverId = receiverId }) > 0));

        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId) =>
            await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteScalarAsync<int>(
                    _messageSqlGenerator.GenerateUnreadCount(currentUserId, senderId).Sql,
                    new { CurrentUserId = currentUserId, SenderId = senderId })));
    }
}