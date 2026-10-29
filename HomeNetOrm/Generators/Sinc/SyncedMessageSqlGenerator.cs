using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Sinc
{
    // 🔥 Наследуемся от нашего универсального MessageSqlGenerator, получая всё его Fluent-ядро!
    internal class SyncedMessageSqlGenerator : MessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedMessageSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Select).CountOne().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(m => m.Id).Emit(OpType.Equal).Param(m => m.Id, id)
                .Build();

        public SqlQueryCommand GenerateSelectUnsynced() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(m => m.IsSynced).Emit(OpType.Equal).Emit(OpType.False)
                .Emit(OpType.Or)
                    .Col(m => m.IsSynced).Emit(OpType.IsNull)
                .Build();

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).Col(m => m.IsSynced).Emit(OpType.Equal).Emit(OpType.True)
                .Emit(PhraseType.Where).Col(m => m.Id).Emit(OpType.Equal).Param(m => m.Id, id)
                .Build();

        // Серверный метод синхронизации — чистый секс без хардкода строки параметров!
        public SqlQueryCommand GenerateSelectMessagesForSync(Guid senderId, DateTimeOffset timestamp) =>
            CreateQuery()
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
}
