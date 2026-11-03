using System.Data.Common;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNetOrm.Interfaces.Repositories
{
    // 🔥 Корневой интерфейс для ЛЮБОГО репозитория в системе. Поставляет пул (фабрику).
    public interface IRepository
    {
      public  ILogger Logger { get; }

        // Позволяет экстеншенам синхронизации безопасно выполнять команды внутри using-блоков пула
        Task ExecuteAsync(Func<DbConnection, Task> command);
        Task<TResult> ExecuteAsync<TResult>(Func<DbConnection, Task<TResult>> query);
    }
}
