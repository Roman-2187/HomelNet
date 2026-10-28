using System;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.Generators
{
    internal class PostgresMessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        protected readonly string IdColumn;

        public PostgresMessageSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger)
        {
            IdColumn = Schema.SinglePrimaryKey?.Name ?? "id";
        }

        public override SqlQueryCommand GenerateInsert() =>
            CreateQuery()
                .Emit(Sql.Cmd.Insert).Emit(Sql.Phrase.Into).Table()
                .InsertFields()
                .Emit(Sql.Phrase.Values)
                .InsertParameters()
                .Emit(Sql.Phrase.Returning).Col(nameof(MessageEntity.Id))
                .Build();

        public override SqlQueryCommand GenerateUpdate() =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).SetFields()
                .Emit(Sql.Phrase.Where).Col(nameof(MessageEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.Id), DapperParams[nameof(MessageEntity.Id)])
                .Build();

        public override SqlQueryCommand GenerateDelete(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Delete).Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).ColAsText(nameof(MessageEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.Id), id.ToString())
                .Build();

        public override SqlQueryCommand GenerateSelectById(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Col(nameof(MessageEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.Id), id)
                .Build();

        public override SqlQueryCommand GenerateSelectAll() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Build();

        public SqlQueryCommand GenerateSelectChatHistory(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                .Emit(Sql.Op.OpenBracket)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.SenderId)}1", senderId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.ReceiverId)}1", receiverId)
                .Emit(Sql.Op.CloseBracket)
                .Emit(Sql.Op.Or)
                .Emit(Sql.Op.OpenBracket)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.SenderId)}2", receiverId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.ReceiverId)}2", senderId)
                .Emit(Sql.Op.CloseBracket)
                .Emit(Sql.Phrase.OrderBy).Col(nameof(MessageEntity.CreatedAt)).Emit(Sql.Op.Asc)
                .Build();

        public SqlQueryCommand GenerateMarkAsRead(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).Col(nameof(MessageEntity.IsRead)).Emit(Sql.Op.Equal).Emit(Sql.Op.True)
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.SenderId), senderId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.ReceiverId), receiverId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.IsRead)).Emit(Sql.Op.Equal).Emit(Sql.Op.False)
                .Build();

        public SqlQueryCommand GenerateUnreadCount(Guid currentUserId, Guid friendId) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).CountAll().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.ReceiverId), currentUserId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.SenderId), friendId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.IsRead)).Emit(Sql.Op.Equal).Emit(Sql.Op.False)
                .Build();

        public SqlQueryCommand GenerateClearChatHistory(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(Sql.Cmd.Delete).Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                .Emit(Sql.Op.OpenBracket)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.SenderId)}1", senderId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.ReceiverId)}1", receiverId)
                .Emit(Sql.Op.CloseBracket)
                .Emit(Sql.Op.Or)
                .Emit(Sql.Op.OpenBracket)
                    .Col(nameof(MessageEntity.SenderId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.SenderId)}2", receiverId)
                    .Emit(Sql.Op.And)
                    .Col(nameof(MessageEntity.ReceiverId)).Emit(Sql.Op.Equal).Param($"{nameof(MessageEntity.ReceiverId)}2", senderId)
                .Emit(Sql.Op.CloseBracket)
                .Build();

        public  object PrepareParameters(MessageEntity msg) => msg;
    }
}
