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
    // 🔥 Добавляем IDisposable для отписки от шины
    public class MessageRepository : BaseRepository<MessageEntity>, IMessageRepo, IDisposable
    {
        private readonly IMessageSqlGenerator _messageSqlGenerator;
        private readonly IEventBus? _eventBus;

        // 🖥️ СЕРВЕРНЫЙ КОНСТРУКТОР (без шины)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ICastomLogger logger)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _messageSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        // 📱 КЛИЕНТСКИЙ КОНСТРУКТОР (с шиной)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ICastomLogger logger, IEventBus eventBus)
            : this(connectionFactory, sqlGenerator, logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
           // SubscribeToEvents();
        }

        // =========================================================================
        // 📥 ПОДПИСКА И ОТПИСКА
        // =========================================================================

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<IMessageRepo.SaveRequested>(OnSaveRequested);
            _eventBus.Subscribe<IMessageRepo.UpdateRequested>(OnUpdateRequested);
            _eventBus.Subscribe<IMessageRepo.DeleteRequested>(OnDeleteRequested);
            _eventBus.Subscribe<IMessageRepo.ClearChatRequested>(OnClearChatRequested);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<IMessageRepo.SaveRequested>(OnSaveRequested);
            _eventBus.Unsubscribe<IMessageRepo.UpdateRequested>(OnUpdateRequested);
            _eventBus.Unsubscribe<IMessageRepo.DeleteRequested>(OnDeleteRequested);
            _eventBus.Unsubscribe<IMessageRepo.ClearChatRequested>(OnClearChatRequested);
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
        }

        // =========================================================================
        // 📥 ОБРАБОТЧИКИ КОМАНД (Просто вызывают чистые методы)
        // =========================================================================

        private async void OnSaveRequested(IMessageRepo.SaveRequested cmd)
        {
            if (cmd?.Message == null) return;
            try
            {
                await InsertAsync(cmd.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageRepository] Ошибка обработки SaveRequested: {ex.Message}");
            }
        }

        private async void OnUpdateRequested(IMessageRepo.UpdateRequested cmd)
        {
            if (cmd?.Message == null) return;
            try
            {
                await UpdateAsync(cmd.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageRepository] Ошибка обработки UpdateRequested: {ex.Message}");
            }
        }

        private async void OnDeleteRequested(IMessageRepo.DeleteRequested cmd)
        {
            if (cmd == null || cmd.MessageId == Guid.Empty) return;
            try
            {
                await DeleteByIdAsync(cmd.MessageId);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageRepository] Ошибка обработки DeleteRequested: {ex.Message}");
            }
        }

        private async void OnClearChatRequested(IMessageRepo.ClearChatRequested cmd)
        {
            if (cmd == null) return;
            try
            {
                await ClearChatHistoryAsync(cmd.SenderId, cmd.ReceiverId);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageRepository] Ошибка обработки ClearChatRequested: {ex.Message}");
            }
        }

        // =========================================================================
        // 🔥 РЕАЛИЗАЦИЯ КОНТРАКТА ICrudRepository (Явные generic-публикации)
        // =========================================================================

        public async Task<MessageEntity> InsertAsync(MessageEntity message)
        {
            if (message.Id == Guid.Empty) message.Id = Guid.NewGuid();

            var cmd = _messageSqlGenerator.GenerateInsert();
            try
            {
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));
                Logger.LogInfo($"[MessageRepository] Сообщение '{message.Id}' успешно записано.");

                // 🔥 Добавили явный generic-параметр, чтобы инспектор железно его распарсил
                _eventBus?.Publish<IMessageRepo.Saved>(this, new IMessageRepo.Saved(message));
                return message;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка ОРМ вставки сообщения: {ex.Message}");
            }
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var cmd = _messageSqlGenerator.GenerateUpdate();
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity));

            if (rowsAffected > 0)
            {
                // 🔥 Добавили явный generic-параметр
                _eventBus?.Publish<IMessageRepo.Updated>(this, new IMessageRepo.Updated(entity));
            }
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            var cmd = _messageSqlGenerator.GenerateDelete(id);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                // 🔥 Добавили явный generic-параметр
                _eventBus?.Publish<IMessageRepo.Deleted>(this, new IMessageRepo.Deleted(id));
            }
        }

        public async Task<MessageEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _messageSqlGenerator.GenerateSelectById(id);
            return await ExecuteAsync(db => db.QueryFirstOrDefaultAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

        public async Task<IEnumerable<MessageEntity>> GetAllAsync()
        {
            var cmd = _messageSqlGenerator.GenerateSelectAll();
            return await ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

        // =========================================================================
        // 🔍 СПЕЦИФИЧНЫЕ МЕТОДЫ
        // =========================================================================

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            var cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                // 🔥 Добавили явный generic-параметр для удаления чата
                _eventBus?.Publish<IMessageRepo.ChatCleared>(this, new IMessageRepo.ChatCleared(senderId, receiverId));
            }

            return rowsAffected > 0;
        }

        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(Guid senderId, Guid receiverId) =>
            await ExecuteAsync(db => db.QueryAsync<MessageEntity>(
                _messageSqlGenerator.GenerateSelectChatHistory(senderId, receiverId).Sql,
                new { SenderId = senderId, ReceiverId = receiverId }));

        public async Task<bool> MarkAsReadAsync(Guid senderId, Guid receiverId) =>
            await ExecuteAsync(async db => await db.ExecuteAsync(
                _messageSqlGenerator.GenerateMarkAsRead(senderId, receiverId).Sql,
                new { SenderId = senderId, ReceiverId = receiverId }) > 0);

        public async Task<int> GetUnreadCountAsync(Guid currentUserId, Guid senderId) =>
            await ExecuteAsync(db => db.ExecuteScalarAsync<int>(
                _messageSqlGenerator.GenerateUnreadCount(currentUserId, senderId).Sql,
                new { CurrentUserId = currentUserId, SenderId = senderId }));
    }
}
