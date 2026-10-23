using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// Специализированный ОРМ-контракт генератора для сущности Пользователя.
    /// Объединяет базовый CRUD и специфичные выборки.
    /// </summary>
    public interface IUserSqlGenerator : ISqlGenerator<UserEntity>
    {
        string GenerateSelectByEmail();
        string GenerateEmailExists();
    }
}
