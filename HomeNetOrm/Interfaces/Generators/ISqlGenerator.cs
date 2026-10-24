using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    internal interface ISqlGenerator<T> where T : class
    {
        SqlQueryCommand GenerateInsert();
        SqlQueryCommand GenerateUpdate();
        SqlQueryCommand GenerateDelete(Guid id); // 🎯 Теперь принимает ID, чтобы завернуть в команду
        SqlQueryCommand GenerateSelectById(Guid id); // 🎯 Тоже принимает ID
        SqlQueryCommand GenerateSelectAll();
        object PrepareParameters(T entity);
    }
}
