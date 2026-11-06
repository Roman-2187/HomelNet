using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Base
{
    // 🔥 Класс чист от репликации, занимается только доменной логикой пользователей
    internal class UserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public UserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
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
