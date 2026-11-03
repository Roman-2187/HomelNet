using System;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Расширяем публичный базовый интерфейс генераторов сущностей
   internal static class BaseSyncSqlGeneratorExtensions
    {
        public static SqlQueryCommand GenerateCountById<T>(this ISqlGenerator<T> generator, Guid id) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).CountOne().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(e => e.Id).Emit(OpType.Equal).Param(e => e.Id, id)
                .Build();
        }

        public static SqlQueryCommand GenerateSelectUnsynced<T>(this ISqlGenerator<T> generator) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(e => e.IsSynced).Emit(OpType.Equal).Emit(OpType.False)
                .Emit(OpType.Or)
                    .Col(e => e.IsSynced).Emit(OpType.IsNull)
                .Build();
        }

        public static SqlQueryCommand GenerateUpdateMarkAsSynced<T>(this ISqlGenerator<T> generator, Guid id) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).Col(e => e.IsSynced).Emit(OpType.Equal).Emit(OpType.True)
                .Emit(PhraseType.Where).Col(e => e.Id).Emit(OpType.Equal).Param(e => e.Id, id)
                .Build();
        }

        public static SqlQueryCommand GenerateSelectUpdatesByTimestamp<T>(this ISqlGenerator<T> generator, DateTimeOffset timestamp) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(e => e.UpdatedAt).Emit(OpType.GreaterThan).Param(e => e.UpdatedAt, timestamp)
                .Emit(PhraseType.OrderBy).Col(e => e.UpdatedAt).Emit(OpType.Asc)
                .Build();
        }
    }
}
