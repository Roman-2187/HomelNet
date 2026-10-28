using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System;

namespace HomeNetOrm.DBProviders.Postgres.SincedGenerators
{
    internal class SyncedPostgresUserSqlGenerator : PostgresUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedPostgresUserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).CountOne().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Raw(IdColumn).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id)
                .Build();

        public SqlQueryCommand GenerateSelectUnsynced() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                .Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.False)
                .Emit(Sql.Op.Or)
                .Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.IsNull)
                .Build();

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.True)
                .Emit(Sql.Phrase.Where).Raw(IdColumn).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id)
                .Build();

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.UpdatedAt)).Emit(Sql.Op.GreaterThan).Param(nameof(UserEntity.UpdatedAt), timestamp)
                .Build();
    }
}
