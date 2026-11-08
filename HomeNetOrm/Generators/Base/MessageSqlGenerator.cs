using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Base
{
    internal class MessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        public MessageSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
            : base(schemas, translator, logger)
        {
        }

        public SqlQueryCommand GenerateSelectChatHistory(Guid senderId, Guid receiverId) =>
    CreateQuery()
        .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
        .Emit(PhraseType.Where)
        .Emit(OpType.OpenBracket)
            // Используем просто @SenderId и @ReceiverId
            .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, senderId)
            .Emit(OpType.And)
            .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, receiverId)
        .Emit(OpType.CloseBracket)
        .Emit(OpType.Or)
        .Emit(OpType.OpenBracket)
            // И тут используем ТЕ ЖЕ САМЫЕ @SenderId и @ReceiverId (только меняем местами)
            .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, receiverId)
            .Emit(OpType.And)
            .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, senderId)
        .Emit(OpType.CloseBracket)
        .Emit(PhraseType.OrderBy).Col(m => m.LocalCreatedAt).Emit(OpType.Asc)
        .Build();

        public SqlQueryCommand GenerateMarkAsRead(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).Col(m => m.IsRead).Emit(OpType.Equal).Emit(OpType.True)
                .Emit(PhraseType.Where)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, senderId)
                    .Emit(OpType.And)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, receiverId)
                    .Emit(OpType.And)
                    .Col(m => m.IsRead).Emit(OpType.Equal).Emit(OpType.False)
                .Build();

        public SqlQueryCommand GenerateUnreadCount(Guid currentUserId, Guid friendId) =>
            CreateQuery()
                .Emit(CmdType.Select).CountAll().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, currentUserId)
                    .Emit(OpType.And)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, friendId)
                    .Emit(OpType.And)
                    .Col(m => m.IsRead).Emit(OpType.Equal).Emit(OpType.False)
                .Build();

        public SqlQueryCommand GenerateClearChatHistory(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(CmdType.Delete).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                .Emit(OpType.OpenBracket)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, "1", senderId)
                    .Emit(OpType.And)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, "1", receiverId)
                .Emit(OpType.CloseBracket)
                .Emit(OpType.Or)
                .Emit(OpType.OpenBracket)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, "2", receiverId)
                    .Emit(OpType.And)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, "2", senderId)
                .Emit(OpType.CloseBracket)
                .Build();

        public object PrepareParameters(MessageEntity msg) => msg;
    }
}
