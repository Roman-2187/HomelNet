using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Interfaces.DbInfrastructure
{
    internal interface ISyncSupport<TEntity> where TEntity : class
    {
        DbConnection Connection { get; }
        ISqlGenerator<TEntity> SqlGenerator { get; }

        ILogger Logger { get; } // 🎯 Добавляем логгер в контракт!
    }

}
