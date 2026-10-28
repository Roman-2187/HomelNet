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
    internal class PostgresUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        protected readonly string IdColumn;

        public PostgresUserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
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
                .Emit(Sql.Phrase.Returning).Col(nameof(UserEntity.Id))
                .Build();

        public override SqlQueryCommand GenerateUpdate() =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).SetFields()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), DapperParams[nameof(UserEntity.Id)])
                .Build();

        public override SqlQueryCommand GenerateDelete(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Delete).Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).ColAsText(nameof(UserEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id.ToString())
                .Build();

        public override SqlQueryCommand GenerateSelectById(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id)
                .Build();

        public override SqlQueryCommand GenerateSelectAll() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Build();

        public SqlQueryCommand GenerateSelectByEmail(string email) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Email)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Email), email)
                .Build();

        public SqlQueryCommand GenerateEmailExists(string email) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).CountAll().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Email)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Email), email)
                .Build();

        public  object PrepareParameters(UserEntity entity) => entity;
    }
}
