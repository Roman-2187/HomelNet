using HomeNetOrm.Builders;

namespace HomeNetOrm.Interfaces.Generators
{
    // 🔥 Абсолютно чистый интерфейс. Больше никаких жестких CRUD-методов!
    internal interface ISqlGenerator<T> where T : class
    {
        TokenQueryBuilder<T> CreateQuery();
    }
}
