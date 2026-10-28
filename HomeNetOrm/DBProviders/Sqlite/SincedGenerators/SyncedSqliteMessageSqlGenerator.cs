using System;
using System.Collections.Generic;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SincedGenerators
{
    internal class SyncedSqliteMessageSqlGenerator : Generators.SqliteMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedSqliteMessageSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).CountOne().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Raw(IdColumn).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.Id), id)
                .Build();

        public SqlQueryCommand GenerateSelectUnsynced() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(MessageEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.False)
                .Emit(Sql.Op.Or)
                    .Col(nameof(MessageEntity.IsSynced)).Emit(Sql.Op.IsNull)
                .Build();

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).Col(nameof(MessageEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.True)
                .Emit(Sql.Phrase.Where).Raw(IdColumn).Emit(Sql.Op.Equal).Param(nameof(MessageEntity.Id), id)
                .Build();
    }
}
