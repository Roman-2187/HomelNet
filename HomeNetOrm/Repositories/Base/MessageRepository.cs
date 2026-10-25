using Dapper;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.DbInfrastructure;
using System.Data.Common;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Models;

namespace HomeNetOrm.Repositories.Base
{
    /// <summary>
    /// Автономный репозиторий сообщений SiberNet.
    /// Работает на базе Guid, изолирован от IDbEngine и использует дефолтную реализацию шины! 🦾
    /// </summary>
    public class MessageRepository : IMessageRepository, ISyncSupport<MessageEntity>
    {
        private readonly DbConnection _connection;
        private readonly IMessageSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        ILogger ISyncSupport<MessageEntity>.Logger => _logger;
        DbConnection ISyncSupport<MessageEntity>.Connection => _connection;
        ISqlGenerator<MessageEntity> ISyncSupport<MessageEntity>.SqlGenerator => _sqlGenerator;

        // 🔥 ФИКС ВИЗИМОСТИ: Конструктор становится internal, чтобы сохранить инкапсуляцию генератора!
        internal MessageRepository(DbConnection connection, IMessageSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsSynced = 0;

            SqlQueryCommand cmd = _sqlGenerator.GenerateInsert();

            try
            {
                object safeParameters = _sqlGenerator.PrepareParameters(message);
                await _connection.ExecuteAsync(cmd.Sql, safeParameters);

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
            // 🎯 ЧИСТЫЙ ВЫЗОВ ЧЕРЕЗ ТОЧКУ: Передаем параметры в аргументы генератора
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectChatHistory(senderId, receiverId);
            try
            {
                return await _connection.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить историю чата через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateMarkAsRead(senderId, receiverId);
            try
            {
                int rowsAffected = await _connection.ExecuteAsync(cmd.Sql, cmd.Params);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления статуса прочтения сообщений через ОРМ: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateUnreadCount(currentUserId, senderId);
            try
            {
                return await _connection.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка подсчета непрочитанных через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> RemoveMessageByIdAsync(Guid id)
        {
            // Получаем сообщение по ID через автопилот команд
            var selectCmd = _sqlGenerator.GenerateSelectById(id);
            var message = await _connection.QueryFirstOrDefaultAsync<MessageEntity>(selectCmd.Sql, selectCmd.Params);

            SqlQueryCommand deleteCmd = _sqlGenerator.GenerateDelete(id);
            try
            {
                int rowsAffected = await _connection.ExecuteAsync(deleteCmd.Sql, deleteCmd.Params);

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

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            try
            {
                int rowsAffected = await _connection.ExecuteAsync(cmd.Sql, cmd.Params);

                if (rowsAffected > 0)
                {
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.ChatCleared(senderId, receiverId));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при полной очистке чата через ОРМ: {ex.Message}");
            }
        }

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectById(id);
            return await _connection.QueryFirstOrDefaultAsync<MessageEntity>(cmd.Sql, cmd.Params);
        }

        public async Task<IEnumerable<MessageEntity>> GetAllAsync()
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectAll();
            return await _connection.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params);
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateUpdate();
            object parameters = _sqlGenerator.PrepareParameters(entity);
            await _connection.ExecuteAsync(cmd.Sql, parameters);
        }

        Task<List<MessageEntity>> IRepository<MessageEntity>.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        private class NullEventProxy : IEventBus { }
    }
}
