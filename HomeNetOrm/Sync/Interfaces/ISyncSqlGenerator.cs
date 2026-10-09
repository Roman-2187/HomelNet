using HomeNetOrm.Interfaces;

namespace HomeNetOrm.Sync.Interfaces
{
    /// <summary>
    /// Подключаемый интерфейс-модуль для генераторов, поддерживающих репликацию SiberNet.
    /// Расширяет базовый CRUD-интерфейс ОРМ.
    /// </summary>
    public interface ISyncSqlGenerator<TEntity> : ISqlGenerator<TEntity> where TEntity : class
    {
        string GenerateCountById();
        string GenerateSelectUnsynced();
        string GenerateUpdateMarkAsSynced();
    }
}
