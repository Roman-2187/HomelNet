using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Sinc
{
    internal class SyncedUserSqlGenerator : UserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedUserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Select).CountOne().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(u => u.Id).Emit(OpType.Equal).Param(u => u.Id, id)
                .Build();

        public SqlQueryCommand GenerateSelectUnsynced() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(u => u.IsSynced).Emit(OpType.Equal).Emit(OpType.False)
                .Emit(OpType.Or)
                    .Col(u => u.IsSynced).Emit(OpType.IsNull)
                .Build();

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).Col(u => u.IsSynced).Emit(OpType.Equal).Emit(OpType.True)
                .Emit(PhraseType.Where).Col(u => u.Id).Emit(OpType.Equal).Param(u => u.Id, id)
                .Build();

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(u => u.UpdatedAt).Emit(OpType.GreaterThan).Param(u => u.UpdatedAt, timestamp)
                .Build();
    }
}
