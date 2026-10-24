using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// Подключаемый интерфейс-модуль для генераторов, поддерживающих репликацию SiberNet.
    /// Полностью переведен на Command Object под вызовы через точку! 🦾
    /// </summary>
    internal interface ISyncSqlGenerator<TEntity> : ISqlGenerator<TEntity> where TEntity : class
    {
        SqlQueryCommand GenerateCountById(Guid id);
        SqlQueryCommand GenerateSelectUnsynced();
        SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id);

     
    }
}
