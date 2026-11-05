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
        private readonly IInternalEventBus? _eventBus;

        // 🖥️ СЕРВЕРНЫЙ КОНСТРУКТОР (без шины)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ILogger logger)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _messageSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
         
        }

        // 📱 КЛИЕНТСКИЙ КОНСТРУКТОР (с шиной)
        internal MessageRepository(Func<DbConnection> connectionFactory, IMessageSqlGenerator sqlGenerator, ILogger logger, IInternalEventBus eventBus)
            : this(connectionFactory, sqlGenerator, logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            SubscribeToEvents();
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
        // 📥 ОБРАБОТЧИКИ КОМАНД
        // =========================================================================

        private async void OnSaveRequested(IMessageRepo.SaveRequested cmd)
        {
            if (cmd?.Message == null) return;
            try { await InsertAsync(cmd.Message); }
            catch (Exception ex) { Logger.LogError($"[MessageRepository] Ошибка SaveRequested: {ex.Message}"); }
        }

        private async void OnUpdateRequested(IMessageRepo.UpdateRequested cmd)
        {
            if (cmd?.Message == null) return;
            try { await UpdateAsync(cmd.Message); }
            catch (Exception ex) { Logger.LogError($"[MessageRepository] Ошибка UpdateRequested: {ex.Message}"); }
        }

        private async void OnDeleteRequested(IMessageRepo.DeleteRequested cmd)
        {
            if (cmd == null || cmd.MessageId == Guid.Empty) return;
            try { await DeleteByIdAsync(cmd.MessageId); }
            catch (Exception ex) { Logger.LogError($"[MessageRepository] Ошибка DeleteRequested: {ex.Message}"); }
        }

        private async void OnClearChatRequested(IMessageRepo.ClearChatRequested cmd)
        {
            if (cmd == null) return;
            try { await ClearChatHistoryAsync(cmd.SenderId, cmd.ReceiverId); }
            catch (Exception ex) { Logger.LogError($"[MessageRepository] Ошибка ClearChatRequested: {ex.Message}"); }
        }

        // =========================================================================
        // 🔥 РЕАЛИЗАЦИЯ КОНТРАКТА ICrudRepository (публикует ФАКТЫ)
        // =========================================================================

        public async Task<MessageEntity> InsertAsync(MessageEntity message)
        {
            if (message.Id == Guid.Empty) message.Id = Guid.NewGuid();

            var cmd = _messageSqlGenerator.GenerateInsert();
            await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));
            Logger.LogInfo($"[MessageRepository] Сообщение '{message.Id}' успешно записано.");

            // 🔥 Публикуем ФАКТ
            _eventBus?.PublishFact(this, new IMessageRepo.Saved(message));
            return message;
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

        public async Task DeleteByIdAsync(Guid id)
        {
            var cmd = _messageSqlGenerator.GenerateDelete(id);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                // 🔥 Публикуем ФАКТ
                _eventBus?.PublishFact(this, new IMessageRepo.Deleted(id));
            }
        }

        public async Task UpdateAsync(MessageEntity entity)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var cmd = _messageSqlGenerator.GenerateUpdate();
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity));

            if (rowsAffected > 0)
            {
                // 🔥 Публикуем ФАКТ
                _eventBus?.PublishFact(this, new IMessageRepo.Updated(entity));
            }
        }

        // =========================================================================
        // 🔍 СПЕЦИФИЧНЫЕ МЕТОДЫ (без публикации фактов, просто чтение/очистка)
        // =========================================================================

        public async Task<bool> ClearChatHistoryAsync(Guid senderId, Guid receiverId)
        {
            var cmd = _messageSqlGenerator.GenerateClearChatHistory(senderId, receiverId);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                // 🔥 Публикуем ФАКТ
                _eventBus?.PublishFact(this, new IMessageRepo.ChatCleared(senderId, receiverId));
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