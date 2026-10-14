using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Sinc;
using System.Data.Common;

namespace HomeNetOrm.Sinc
{
    /// <summary>
    /// Абстрактный фундамент семейства синхронизаторов SiberNet.
    /// Работает строго через абстрактные интерфейсы IDbEngine! 🧼
    /// </summary>
    public abstract class BaseSynchronizer<TEntity> : ISiberNetSyncCoordinator, IDisposable
        where TEntity : class
    {
       


        protected readonly IDbEngine LocalEngine;
        protected readonly IDbEngine RemoteEngine;
        protected readonly IDbContextContainer ContextContainer;
        protected readonly IEventBus EventBus;
        protected readonly ILogger Logger;

        private bool _isInitialSyncExecuted;

        protected BaseSynchronizer(
            IDbEngine localEngine,
            IDbEngine remoteEngine,
            IDbContextContainer contextContainer,
            IEventBus eventBus,
            ILogger logger)
        {
            LocalEngine = localEngine ?? throw new ArgumentNullException(nameof(localEngine));
            RemoteEngine = remoteEngine ?? throw new ArgumentNullException(nameof(remoteEngine));
            ContextContainer = contextContainer ?? throw new ArgumentNullException(nameof(contextContainer));
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        /// <summary>
        /// Каждый дочерний синхронизатор сам решает, на какие именно рекорды шины ему подписываться
        /// </summary>
        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();
        protected abstract Task ExecuteTableSyncInternalAsync(DbConnection local, DbConnection remote);

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            if (!ContextContainer.IsPostgresAvailable)
            {
                Logger.LogWarning($"[{GetType().Name}] Центральный сервер недоступен. Пакетная сверка пропущена.");
                return;
            }

            // 🎯 ШАГ 1: Ставим семафор в САМУЮ ПЕРВУЮ СТРОКУ метода!
            // Ни один поток не сдвинется дальше этой точки, пока первый полностью не выйдет!
            await PostgresSyncLock.Instance.WaitAsync();

            try
            {
                // 🎯 ШАГ 2: Только ПОСЛЕ захвата семафора безопасно пишем лог и трогаем подключения
                Logger.LogInfo($"=== [{GetType().Name}] ЗАПУСК ПАКЕТНОЙ СВЕРКИ СУБД ===");

                var localConn = LocalEngine.Connection;
                var remoteConn = RemoteEngine.Connection;

                if (localConn.State != System.Data.ConnectionState.Open) await localConn.OpenAsync();
                if (remoteConn.State != System.Data.ConnectionState.Open) await remoteConn.OpenAsync();

                await ExecuteTableSyncInternalAsync(localConn, remoteConn);

                Logger.LogInfo($"=== [{GetType().Name}] ПАКЕТНАЯ СВЕРКА УСПЕШНО ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{GetType().Name}] Катастрофа пакетной синхронизации: {ex.Message}");
            }
            finally
            {
                // 🎯 ШАГ 3: Железобетонно освобождаем турникет для следующего класса
                PostgresSyncLock.Instance.Release();
            }
        }


        protected void ExecuteSyncTask(Func<DbConnection, Task> syncAction, string operationName)
        {
            if (!ContextContainer.IsPostgresAvailable) return;

            Task.Run(async () =>
            {
                await PostgresSyncLock.Instance.WaitAsync();
                try
                {
                    var remoteConn = RemoteEngine.Connection;
                    if (remoteConn.State != System.Data.ConnectionState.Open) await remoteConn.OpenAsync();
                    await syncAction(remoteConn);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[{GetType().Name}] Сбой операции '{operationName}' при репликации: {ex.Message}");
                }
                finally
                {
                    PostgresSyncLock.Instance.Release();
                }
            });
        }

        public virtual void Dispose()
        {
            UnsubscribeEvents();
            PostgresSyncLock.Instance.Dispose();
        }
    }


    /// <summary>
    /// Единый глобальный турникет для всех потоков репликации СУБД.
    /// Защищает общий провод DbConnection от параллельных запросов! 🦾
    /// </summary>
    public static class PostgresSyncLock
    {
        public static readonly SemaphoreSlim Instance = new SemaphoreSlim(1, 1);
    }
}
