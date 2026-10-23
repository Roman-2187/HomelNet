using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    internal abstract class BaseSqlGenerator<T> : CoreSqlGenerator<T> where T : class
    {
        protected BaseSqlGenerator(IEnumerable<TableSchema> availableSchemas, NameFormat databaseFormat, ILogger logger)
            : base(availableSchemas, databaseFormat, logger) { }

        public abstract string GenerateInsert();
        public abstract string GenerateUpdate();
        public abstract string GenerateDelete();
        public abstract string GenerateSelectById();
        public abstract string GenerateSelectAll();
        public virtual object PrepareParameters(T entity) => entity;
    }
}
