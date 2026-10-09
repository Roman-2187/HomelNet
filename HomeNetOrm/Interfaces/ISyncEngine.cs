using System.Data.Common;

namespace HomeNetOrm.Interfaces
{
    /// <summary>
    /// Контракт универсального движка синхронизации SiberNet для работы в координаторе.
    /// </summary>
    public interface ISyncEngine
    {
        Task PullAsync(DbConnection sqlite, DbConnection pg);
        Task PushAsync(DbConnection sqlite, DbConnection pg);
    }
}
