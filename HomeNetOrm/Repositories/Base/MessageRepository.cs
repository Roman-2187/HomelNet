using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Models;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNetOrm.Repositories.Base
{
    public class MessageRepository : BaseRepository<MessageEntity>, IMessageRepository
    {
        private readonly IMessageSqlGenerator _messageSqlGenerator;
        private readonly IEventBus _eventBus;

        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _messageSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();
        }

        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsSynced = 0;

            SqlQueryCommand cmd = _messageSqlGenerator.GenerateInsert();

            try
            {
                object safeParameters = _messageSqlGenerator.PrepareParameters(message);

                Task DbAction(DbConnection db)
                {
                    return db.ExecuteAsync(cmd.Sql, safeParameters);
                }

                await ExecuteAsync(DbAction);

                _eventBus.Publish(this, new ISyncCoordinator.MessageInserted(message));
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения сообщения через ОРМ: {ex.Message}");
            }
        }

        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateSelectChatHistory(senderId, receiverId);
            try
            {
                Task<IEnumerable<MessageEntity>> DbAction(DbConnection db)
                {
                    return db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params);
                }

                return await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить историю чата через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateMarkAsRead(senderId, receiverId);
            try
            {
                async Task<bool> DbAction(DbConnection db)
                {
                    int rowsAffected = await db.ExecuteAsync(cmd.Sql, cmd.Params);
                    return rowsAffected > 0;
                }

                return await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления статуса прочтения сообщений через ОРМ: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateUnreadCount(currentUserId, senderId);
            try
            {
                Task<int> DbAction(DbConnection db)
                {
                    return db.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
                }

                return await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка подсчета непрочитанных через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> RemoveMessageByIdAsync(Guid id)
        {
            var selectCmd = _messageSqlGenerator.GenerateSelectById(id);
            SqlQueryCommand deleteCmd = _messageSqlGenerator.GenerateDelete(id);

            MessageEntity? deletedMessage = null;
            int rowsAffected = 0;

            try
            {
                async Task DbAction(DbConnection db)
                {
                    deletedMessage = await db.QueryFirstOrDefaultAsync<MessageEntity>(selectCmd.Sql, selectCmd.Params);
                    rowsAffected = await db.ExecuteAsync(deleteCmd.Sql, deleteCmd.Params);
                }

                await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления сообщения по ID через ОРМ: {ex.Message}");
            }

            // Публикация вынесена из замыкания наружу
            if (rowsAffected > 0 && deletedMessage != null)
            {
                _eventBus.Publish(this, new ISyncCoordinator.MessageDeleted(deletedMessage.Id));
            }

            return rowsAffected > 0;
        }

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            int rowsAffected = 0;

            try
            {
                Task<int> DbAction(DbConnection db)
                {
                    return db.ExecuteAsync(cmd.Sql, cmd.Params);
                }

                rowsAffected = await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при полной очистке чата через ОРМ: {ex.Message}");
            }

            // Публикация вынесена из замыкания наружу
            if (rowsAffected > 0)
            {
                _eventBus.Publish(this, new ISyncCoordinator.ChatCleared(senderId, receiverId));
            }

            return rowsAffected > 0;
        }

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateSelectById(id);

            Task<MessageEntity?> DbAction(DbConnection db)
            {
                return db.QueryFirstOrDefaultAsync<MessageEntity>(cmd.Sql, cmd.Params);
            }

            return await ExecuteAsync(DbAction);
        }

        public async Task<List<MessageEntity>> GetAllAsync()
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateSelectAll();

            async Task<List<MessageEntity>> DbAction(DbConnection db)
            {
                var result = await db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params);
                return result.ToList();
            }

            return await ExecuteAsync(DbAction);
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateUpdate();
            object parameters = _messageSqlGenerator.PrepareParameters(entity);

            Task DbAction(DbConnection db)
            {
                return db.ExecuteAsync(cmd.Sql, parameters);
            }

            await ExecuteAsync(DbAction);
        }

        private class NullEventProxy : IEventBus { }
    }
}
