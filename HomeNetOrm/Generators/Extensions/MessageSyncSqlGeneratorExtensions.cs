using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Расширяем публичный интерфейс генератора сообщений
    internal static class MessageSyncSqlGeneratorExtensions
    {
        public static SqlQueryCommand GenerateSelectMessagesForSync(this IMessageSqlGenerator generator, Guid senderId, DateTimeOffset timestamp)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                .Emit(OpType.OpenBracket)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, "1", senderId)
                    .Emit(OpType.Or)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, "1", senderId)
                .Emit(OpType.CloseBracket)
                .Emit(OpType.And)
                    .Col(m => m.UpdatedAt).Emit(OpType.GreaterThan).Param(m => m.UpdatedAt, timestamp)
                .Emit(PhraseType.OrderBy).Col(m => m.UpdatedAt).Emit(OpType.Asc)
                .Build();
        }

        public static SqlQueryCommand GenerateUpsert(this IMessageSqlGenerator generator)
        {
            var baseInsert = generator.GenerateInsert();

            return generator.CreateQuery()
                .Raw(baseInsert.Sql)
                .Emit(PhraseType.On).Emit(PhraseType.Conflict).Emit(OpType.OpenBracket).Col(m => m.Id).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)

                // text = EXCLUDED.text
                .Col(m => m.Text).Emit(OpType.Equal).Emit(OpType.Excluded).Col(m => m.Text).Emit(OpType.Comma)

                // is_read = EXCLUDED.is_read
                .Col(m => m.IsRead).Emit(OpType.Equal).Emit(OpType.Excluded).Col(m => m.IsRead).Emit(OpType.Comma)

                // local_created_at = EXCLUDED.local_created_at
                .Col(m => m.LocalCreatedAt).Emit(OpType.Equal).Emit(OpType.Excluded).Col(m => m.LocalCreatedAt).Emit(OpType.Comma)

                // updated_at = CURRENT_TIMESTAMP
                .Col(m => m.UpdatedAt).Emit(OpType.Equal).Emit(OpType.CurrentTimestamp)
                .Build();
        }
    }
}
