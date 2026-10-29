using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Base
{
    internal class UserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public UserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger)
        {
        }

        public SqlQueryCommand GenerateSelectByEmail(string email) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(u => u.Email).Emit(OpType.Equal).Param(u => u.Email, email)
                .Build();

        public SqlQueryCommand GenerateEmailExists(string email) =>
            CreateQuery()
                .Emit(CmdType.Select).CountAll().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(u => u.Email).Emit(OpType.Equal).Param(u => u.Email, email)
                .Build();

        public object PrepareParameters(UserEntity entity) => entity;
    }
}
