using HomeNetCore.Models;
using HomeNetOrm.Interfaces;

namespace HomeNetOrm.DBProviders.Interfaces
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
