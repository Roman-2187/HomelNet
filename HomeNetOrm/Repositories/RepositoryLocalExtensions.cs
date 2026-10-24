using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    internal static class RepositoryLocalExtensions
    {
        public static async Task<IEnumerable<TEntity>> GetUnsyncedRecordsAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository) where TEntity : BaseEntity
        {
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
            {
                return Array.Empty<TEntity>();
            }

            DbConnection localConn = localRepository.Connection;
            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            SqlQueryCommand cmd = syncGen.GenerateSelectUnsynced();
            return await localConn.QueryAsync<TEntity>(cmd.Sql, cmd.Params);
        }

        public static async Task MarkAsSyncedAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository, Guid id) where TEntity : BaseEntity
        {
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen) return;

            var logger = localRepository.Logger;

            try
            {
                DbConnection localConn = localRepository.Connection;
                if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

                // 🎯 АВТОПИЛОТ: Передаем id в метод. Генератор сам упакует правильный параметр по словарю
                SqlQueryCommand cmd = syncGen.GenerateUpdateMarkAsSynced(id);

                await localConn.ExecuteAsync(cmd.Sql, cmd.Params);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[ОРМ МАРКЕР] [WARN] Споткнулись на фиксации IsSynced для {typeof(TEntity).Name} (ID: {id}). Текст ошибки: {ex.Message}");
            }
        }

        public static async Task<int> MergePulledRecordsAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository,
            IEnumerable<TEntity> remoteRecords,
            IEventBus eventBus,
            HomeNetCore.Interfaces.Diagnostics.ILogger logger) where TEntity : BaseEntity
        {
            if (remoteRecords == null) return 0;

            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
            {
                logger.LogWarning($"[ОРМ МЕРЖ] Таблица {typeof(TEntity).Name} пропущена: генератор СУБД не реализует ISyncSqlGenerator.");
                return 0;
            }

            DbConnection localConn = localRepository.Connection;
            var localGen = localRepository.SqlGenerator;

            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            int insertedCount = 0;
            int skippedCount = 0;
            int totalCount = 0;

            // 🎯 Забираем базовый SQL для вставки (параметры пойдут через record)
            SqlQueryCommand insertCmd = localGen.GenerateInsert();

            using var transaction = await localConn.BeginTransactionAsync();
            try
            {
                foreach (var record in remoteRecords)
                {
                    if (record == null) continue;
                    totalCount++;

                    // 🎯 АВТОПИЛОТ: Каждая итерация запрашивает команду проверки по конкретному record.Id
                    SqlQueryCommand checkCmd = syncGen.GenerateCountById(record.Id);

                    var exists = await localConn.ExecuteScalarAsync<bool>(checkCmd.Sql, checkCmd.Params, transaction);

                    if (!exists)
                    {
                        record.IsSynced = 1;

                        object localParameters = localGen.PrepareParameters(record);

                        await localConn.ExecuteAsync(insertCmd.Sql, localParameters, transaction);
                        insertedCount++;

                        logger.LogDebug($"[ОРМ МЕРЖ -> {typeof(TEntity).Name}] ➕ Вставлена новая запись. ID: {record.Id}");
                    }
                    else
                    {
                        skippedCount++;
                        logger.LogDebug($"[ОРМ МЕРЖ -> {typeof(TEntity).Name}] 💤 Запись уже существует в SQLite, пропуск. ID: {record.Id}");
                    }
                }

                await transaction.CommitAsync();

                logger.LogInfo($"[ОРМ МЕРЖ] Обработка таблицы {typeof(TEntity).Name} завершена. Всего в пакете: {totalCount} | Добавлено: {insertedCount} | Пропущено (уже есть): {skippedCount}");

                if (insertedCount > 0 && typeof(TEntity) == typeof(UserEntity))
                {
                    logger.LogInfo($"[ОРМ МЕРЖ] Публикуем RefreshRequest для авто-обновления таблицы пользователей в UI.");
                    eventBus.Publish(localRepository, new IUsersTableVm.RefreshRequest());
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError($"[ОРМ МЕРЖ] Критическая ошибка при слиянии пакета данных: {ex.Message}");
                throw;
            }

            return insertedCount;
        }
    }
}
