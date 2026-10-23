using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🧱 БАЗОВЫЙ CRUD-ГЕНЕРАТОР: Для стандартных сущностей. 
    /// Реализует интерфейс ОРМ и требует стандартные SQL-методы.
    /// </summary>
    internal abstract class BaseSqlGenerator<T> : CoreSqlGenerator<T>, ISqlGenerator<T> where T : class
    {
        protected BaseSqlGenerator(ISchemaAdapter adapter, IEnumerable<TableSchema> availableSchemas, ILogger logger)
            : base(adapter, availableSchemas, logger) { }

        public abstract string GenerateInsert();
        public abstract string GenerateUpdate();
        public abstract string GenerateDelete();
        public abstract string GenerateSelectById();
        public abstract string GenerateSelectAll();

        public virtual object PrepareParameters(T entity)
        {
            return entity;
        }
    }
}
