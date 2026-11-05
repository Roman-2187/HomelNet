using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    /// <summary>
    /// Чистый контракт для CRUD-операций. Без реализации.
    /// </summary>
    public interface ICrudRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity> InsertAsync(TEntity entity);
        Task<TEntity?> GetByIdAsync(Guid id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task DeleteByIdAsync(Guid id);
        Task UpdateAsync(TEntity entity);
    }
}