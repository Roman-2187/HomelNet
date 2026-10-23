using Dapper;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.DbInfrastructure;
using System.Data.Common;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;

namespace HomeNetOrm.Repositories
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

        private readonly ILogger _logger; // Внедряем поле логгера напрямую

        ILogger ISyncSupport<MessageEntity>.Logger => _logger; 

        // Явная реализация инфраструктурных свойств для методов расширения синхронизации
        DbConnection ISyncSupport<MessageEntity>.Connection => _connection;
        ISqlGenerator<MessageEntity> ISyncSupport<MessageEntity>.SqlGenerator => _sqlGenerator;

        // 🔥 ЕДИНЫЙ УНИВЕРСАЛЬНЫЙ КОНСТРУКТОР ДЛЯ КЛИЕНТА И СЕРВЕРА
        // Если eventBus не передан (на сервере), создается пустой прокси, берущий пустые методы из интерфейса!
        public MessageRepository(DbConnection connection, IMessageSqlGenerator sqlGenerator,ILogger logger ,IEventBus? eventBus = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();

         _logger=logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                // Стало (генератор сам переведет IsSynced и IsRead в честные bool для Postgres):
                object safeParameters = _sqlGenerator.PrepareParameters(message);
                await _connection.ExecuteAsync(sql, safeParameters);

                // На клиенте - вызовет реальную шину. На сервере - метод интерфейса выполнится вхолостую!
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

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            string sql = _sqlGenerator.GenerateClearChatHistory();
            try
            {
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



        // =========================================================================
        // 🦾 ИСПРАВЛЕННАЯ РЕАЛИЗАЦИЯ МЕТОДОВ ИНТЕРФЕЙСА IRepository<MessageEntity>
        // =========================================================================

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            string sql = _sqlGenerator.GenerateSelectById();
            return await _connection.QueryFirstOrDefaultAsync<MessageEntity>(sql, new { Id = id });
        }

        public async Task<IEnumerable<MessageEntity>> GetAllAsync()
        {
            string sql = _sqlGenerator.GenerateSelectAll();
            return await _connection.QueryAsync<MessageEntity>(sql);
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            string sql = _sqlGenerator.GenerateUpdate();
            object parameters = _sqlGenerator.PrepareParameters(entity);
            await _connection.ExecuteAsync(sql, parameters);
        }

        Task<List<MessageEntity>> IRepository<MessageEntity>.GetAllAsync()
        {
            throw new NotImplementedException();
        }



        // Минимальный скрытый приватный прокси-класс
        private class NullEventProxy : IEventBus { }
    }
}
