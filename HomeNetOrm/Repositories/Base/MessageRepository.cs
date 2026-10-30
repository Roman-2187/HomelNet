using Dapper;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Models;

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
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, safeParameters));

                _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));
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
                return await ExecuteAsync(async db => await db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
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
                return await ExecuteAsync(async db =>
                {
                    int rowsAffected = await db.ExecuteAsync(cmd.Sql, cmd.Params);
                    return rowsAffected > 0;
                });
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
                return await ExecuteAsync(async db => await db.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params));
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

            try
            {
                return await ExecuteAsync(async db =>
                {
                    var message = await db.QueryFirstOrDefaultAsync<MessageEntity>(selectCmd.Sql, selectCmd.Params);
                    int rowsAffected = await db.ExecuteAsync(deleteCmd.Sql, deleteCmd.Params);

                    if (rowsAffected > 0 && message != null)
                    {
                        _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageDeleted(message.Id));
                    }

                    return rowsAffected > 0;
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления сообщения по ID через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            try
            {
                return await ExecuteAsync(async db =>
                {
                    int rowsAffected = await db.ExecuteAsync(cmd.Sql, cmd.Params);
                    if (rowsAffected > 0)
                    {
                        _eventBus.Publish(this, new ISiberNetSyncCoordinator.ChatCleared(senderId, receiverId));
                    }
                    return rowsAffected > 0;
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при полной очистке чата через ОРМ: {ex.Message}");
            }
        }

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateSelectById(id);
            return await ExecuteAsync(async db => await db.QueryFirstOrDefaultAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

        // 🎯 Меняем возвращаемый тип на Task<List<MessageEntity>>, как просит интерфейс!
        public async Task<List<MessageEntity>> GetAllAsync()
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateSelectAll();
            return await ExecuteAsync(async db => (await db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params)).ToList());
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            SqlQueryCommand cmd = _messageSqlGenerator.GenerateUpdate();
            object parameters = _messageSqlGenerator.PrepareParameters(entity);
            await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, parameters));
        }

        private class NullEventProxy : IEventBus { }
    }
}
