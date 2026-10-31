using Dapper;
using HomeNetCore.Interfaces.Repositories; // Контракт IMessageRepo из Ядра
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Repositories.Base;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interceptors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetOrm.Repositories
{
    public class MessageRepo : BaseRepository<MessageEntity>, IMessageRepo
    {
        private readonly IMessageSqlGenerator _msgSqlGenerator;
        private readonly IEventBus _eventBus;
        private readonly LogInterceptor _interceptor; // Наш сеньорский страж

        // =========================================================================
        // 🔒 СКРЫТЫЕ ВНУТРЕННИЕ РЕКОРДЫ ДЛЯ ФАКТОВ (Защита от фейковых публикаций снаружи)
        // =========================================================================
        private record Saved(MessageEntity? Message, string? MessageText) : IMessageRepo.ISaved;
        private record Updated(MessageEntity? Message, string? MessageText) : IMessageRepo.IUpdated;
        private record Deleted(MessageEntity? Message, string? MessageText) : IMessageRepo.IDeleted;
        private record HistoryLoaded(Guid SenderId, Guid ReceiverId, IEnumerable<MessageEntity> History) : IMessageRepo.IHistoryLoaded;
        private record MarkedAsRead(Guid SenderId, Guid ReceiverId, string? MessageText) : IMessageRepo.IMarkedAsRead;
        private record UnreadCountCalculated(Guid CurrentUserId, Guid SenderId, int Count) : IMessageRepo.IUnreadCountCalculated;
        private record ChatCleared(Guid SenderId, Guid ReceiverId, string? MessageText) : IMessageRepo.IChatCleared;

        internal MessageRepo(
            Func<DbConnection> connectionFactory,
            IMessageSqlGenerator sqlGenerator,
            ILogger logger,
            IEventBus eventBus,
            LogInterceptor interceptor)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _msgSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));

            // Подключаем репозиторий к «эфиру» шины событий
            InitializeSubscriptions();
        }

        private void InitializeSubscriptions()
        {
            _eventBus.SubscribeAsync<IMessageRepo.SaveRequested>(OnSaveRequested);
            _eventBus.SubscribeAsync<IMessageRepo.UpdateRequested>(OnUpdateRequested);
            _eventBus.SubscribeAsync<IMessageRepo.DeleteRequested>(OnDeleteRequested);
            _eventBus.SubscribeAsync<IMessageRepo.HistoryRequested>(OnHistoryRequested);
            _eventBus.SubscribeAsync<IMessageRepo.MarkAsReadRequested>(OnMarkAsReadRequested);
            _eventBus.SubscribeAsync<IMessageRepo.UnreadCountRequested>(OnUnreadCountRequested);
            _eventBus.SubscribeAsync<IMessageRepo.ClearChatRequested>(OnClearChatRequested);
        }

        // =========================================================================
        // 📥 ОБРАБОТЧИКИ КОМАНД (Вся рутина try-catch и логирования ушла в GuardAsync)
        // =========================================================================

        private async Task OnSaveRequested(IMessageRepo.SaveRequested request)
        {
            var msg = request.Message;
            if (msg.Id == Guid.Empty) msg.Id = Guid.NewGuid();
            msg.UpdatedAt = DateTimeOffset.UtcNow;

            var cmd = _msgSqlGenerator.GenerateInsert();

            var (savedMsg, dbError) = await _interceptor.GuardAsync(this, async () =>
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, msg));
                return msg;
            });

            await _eventBus.PublishAsync(this, new Saved(savedMsg, dbError));
        }

        private async Task OnUpdateRequested(IMessageRepo.UpdateRequested request)
        {
            var msg = request.Message;
            msg.UpdatedAt = DateTimeOffset.UtcNow;

            var cmd = _msgSqlGenerator.GenerateUpdate();

            var (updatedMsg, dbError) = await _interceptor.GuardAsync(this, async () =>
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, msg));
                return msg;
            });

            await _eventBus.PublishAsync(this, new Updated(updatedMsg, dbError));
        }

        private async Task OnDeleteRequested(IMessageRepo.DeleteRequested request)
        {
            // Сначала достаем сообщение из БД, чтобы вернуть полную сущность в факт удалении
            var selectCmd = _msgSqlGenerator.GenerateSelectById(request.MessageId);
            var currentMsg = await ExecuteAsync(async db => await db.QueryFirstOrDefaultAsync<MessageEntity>(selectCmd.Sql, selectCmd.Params));

            if (currentMsg == null)
            {
                await _eventBus.PublishAsync(this, new Deleted(null, "Сообщение не найдено в локальной БД."));
                return;
            }

            var deleteCmd = _msgSqlGenerator.GenerateDelete(request.MessageId);

            var (_, dbError) = await _interceptor.GuardAsync(this, async () =>
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(deleteCmd.Sql, deleteCmd.Params));
                return currentMsg;
            });

            await _eventBus.PublishAsync(this, new Deleted(dbError == null ? currentMsg : null, dbError));
        }

        private async Task OnHistoryRequested(IMessageRepo.HistoryRequested request)
        {
            var cmd = _msgSqlGenerator.GenerateSelectChatHistory(request.SenderId, request.ReceiverId);

            var (history, _) = await _interceptor.GuardAsync(this, async () =>
            {
                var result = await ExecuteAsync(async db => await db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
                return result;
            });

            await _eventBus.PublishAsync(this, new HistoryLoaded(request.SenderId, request.ReceiverId, history ?? Enumerable.Empty<MessageEntity>()));
        }

        private async Task OnMarkAsReadRequested(IMessageRepo.MarkAsReadRequested request)
        {
            var cmd = _msgSqlGenerator.GenerateMarkAsRead(request.SenderId, request.ReceiverId);

            var (_, dbError) = await _interceptor.GuardAsync(this, async () =>
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, cmd.Params));
                return this;
            });

            await _eventBus.PublishAsync(this, new MarkedAsRead(request.SenderId, request.ReceiverId, dbError));
        }

        private async Task OnUnreadCountRequested(IMessageRepo.UnreadCountRequested request)
        {
            var cmd = _msgSqlGenerator.GenerateUnreadCount(request.CurrentUserId, request.SenderId);

            // Боксим анонимный тип, чтобы обойти ограничение class на TResult
            var (countResult, _) = await _interceptor.GuardAsync(this, async () =>
            {
                var count = await ExecuteAsync(async db => await db.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params));
                return new { Count = count };
            });

            int finalCount = countResult?.Count ?? 0;
            await _eventBus.PublishAsync(this, new UnreadCountCalculated(request.CurrentUserId, request.SenderId, finalCount));
        }

        private async Task OnClearChatRequested(IMessageRepo.ClearChatRequested request)
        {
            var cmd = _msgSqlGenerator.GenerateClearChatHistory(request.SenderId, request.ReceiverId);

            var (_, dbError) = await _interceptor.GuardAsync(this, async () =>
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, cmd.Params));
                return this;
            });

            await _eventBus.PublishAsync(this, new ChatCleared(request.SenderId, request.ReceiverId, dbError));
        }
    }
}
