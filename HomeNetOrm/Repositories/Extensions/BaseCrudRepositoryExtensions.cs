using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Generators.Extensions; // Наши CRUD-экстеншены генераторов!
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Extensions
{
    // 🔥 Этот обвес прокачивает любой доменный репозиторий до полноценного CRUD!
    public static class BaseCrudRepositoryExtensions
    {
        public static async Task<TEntity?> GetByIdAsync<TEntity>(this IRepository repo, Guid id) where TEntity : BaseEntity
        {
            var baseRepo = (BaseRepository<TEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateSelectById(id);

            return await baseRepo.ExecuteAsync(db => db.QueryFirstOrDefaultAsync<TEntity>(cmd.Sql, cmd.Params));
        }

        public static async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(this IRepository repo) where TEntity : BaseEntity
        {
            var baseRepo = (BaseRepository<TEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateSelectAll();

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<TEntity>(cmd.Sql, cmd.Params));
        }

        public static async Task DeleteByIdAsync<TEntity>(this IRepository repo, Guid id) where TEntity : BaseEntity
        {
            var baseRepo = (BaseRepository<TEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateDelete(id);

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));
        }

        public static async Task UpdateAsync<TEntity>(this IRepository repo, TEntity entity) where TEntity : BaseEntity
        {
            var baseRepo = (BaseRepository<TEntity>)repo;
            entity.UpdatedAt = DateTimeOffset.UtcNow; // Гарантируем накат свежего таймстампа изменений

            var cmd = baseRepo.SqlGenerator.GenerateUpdate();
            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity));
        }
    }
}
