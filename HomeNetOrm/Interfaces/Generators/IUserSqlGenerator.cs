using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    internal interface IUserSqlGenerator : ISqlGenerator<UserEntity>
    {
        SqlQueryCommand GenerateSelectByEmail(string email);
        SqlQueryCommand GenerateEmailExists(string email);
    }
}
