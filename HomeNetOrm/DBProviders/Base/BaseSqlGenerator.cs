using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    internal abstract class BaseSqlGenerator<T>(IEnumerable<TableSchema> availableSchemas, ILogger logger) : CoreSqlGenerator<T>(availableSchemas, logger) where T : class
    {
        public abstract SqlQueryCommand GenerateInsert();
        public abstract SqlQueryCommand GenerateUpdate();
        public abstract SqlQueryCommand GenerateDelete(Guid id);
        public abstract SqlQueryCommand GenerateSelectById(Guid id);
        public abstract SqlQueryCommand GenerateSelectAll();
        public virtual object PrepareParameters(T entity) => entity;
    }
}

