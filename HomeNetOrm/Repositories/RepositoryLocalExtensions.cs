using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    internal static class RepositoryLocalExtensions
    {
        public static async Task<IEnumerable<TEntity>> GetUnsyncedRecordsAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository) where TEntity : class, ISyncableEntity
        {
            // === РАЗРУЛИВАЕМ ТРАБЛЫ: Проверка на поддержку синхронизации ===
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
            {
                return Array.Empty<TEntity>(); // Просто возвращаем пустоту, не дергая базу
            }

            DbConnection localConn = localRepository.Connection;
            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            string sql = syncGen.GenerateSelectUnsynced(); // Безопасный вызов из syncGen
            return await localConn.QueryAsync<TEntity>(sql);
        }


        /// <summary>
        /// ⚡ УНИВЕРСАЛЬНЫЙ метод отметки записи как синхронизированной. 
        /// Никакого сырого SQL в репозиториях! 🦾
        /// </summary>
        public static async Task MarkAsSyncedAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository, Guid id) where TEntity : class
        {
            // Если генератор не поддерживает синхронизацию — просто выходим
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen) return;

            DbConnection localConn = localRepository.Connection;
            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            // Генератор сам вернет: "SET is_synced = 1" для SQLite или "SET is_synced = TRUE" для Postgres!
            string sql = syncGen.GenerateUpdateMarkAsSynced();

            await localConn.ExecuteAsync(sql, new { Id = id });
        }

        /// <summary>
        /// 📥 Накатывает в локальную базу готовую пачку данных, которую выдал сервер (PULL)
        /// </summary>
        public static async Task<int> MergePulledRecordsAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository,
            IEnumerable<TEntity> remoteRecords,
            IEventBus eventBus,
            HomeNetCore.Interfaces.Diagnostics.ILogger logger) where TEntity : class, ISyncableEntity
        {
            if (remoteRecords == null) return 0;

            // === ЖЕЛЕЗОБЕТОННЫЙ ЩИТ ОТ ПАДЕНИЙ ===
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen)
            {
                logger.LogWarning($"[ОРМ МЕРЖ] Таблица {typeof(TEntity).Name} пропущена: генератор СУБД не реализует ISyncSqlGenerator.");
                return 0;
            }

            DbConnection localConn = localRepository.Connection;
            var localGen = localRepository.SqlGenerator;

            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            int mergedCount = 0;

            string checkLocalSql = syncGen.GenerateCountById();
            string insertLocalSql = localGen.GenerateInsert();

            using var transaction = await localConn.BeginTransactionAsync();
            try
            {
                foreach (var record in remoteRecords)
                {
                    var exists = await localConn.ExecuteScalarAsync<bool>(checkLocalSql, new { id = record.Id }, transaction);
                    if (!exists)
                    {
                        record.IsSynced = 1; // Скачанное изначально синхронизировано

                        // Генератор сам адаптирует параметры (например, bool в 0/1 для SQLite)
                        object localParameters = localGen.PrepareParameters(record);

                        await localConn.ExecuteAsync(insertLocalSql, localParameters, transaction);
                        mergedCount++;
                    }
                }

                await transaction.CommitAsync();

                // === ЛОГИРОВАНИЕ И ПИНОК ДЛЯ АВАЛОНИИ ===
                if (mergedCount > 0)
                {
                    logger.LogInfo($"[ОРМ МЕРЖ] Успешно синхронизировано и записано в локальную БД записей: {mergedCount} (Таблица: {typeof(TEntity).Name})");

                    if (typeof(TEntity) == typeof(UserEntity))
                    {
                        logger.LogInfo($"[ОРМ МЕРЖ] Публикуем RefreshRequest для авто-обновления таблицы пользователей в UI.");
                        // Пуляем твой родной рекорд для TableUsersViewModel! 🎯
                        eventBus.Publish(localRepository, new IUsersTableVm.RefreshRequest());
                    }
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError($"[ОРМ МЕРЖ] Критическая ошибка при слиянии пакета данных: {ex.Message}");
                throw;
            }

            return mergedCount;
        }

    }
}
