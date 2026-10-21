using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
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



        public static async Task MarkAsSyncedAsync<TEntity>( this ISyncSupport<TEntity> localRepository, Guid id) where TEntity : class
       
        {
            if (localRepository.SqlGenerator is not ISyncSqlGenerator<TEntity> syncGen) return;

            var logger = localRepository.Logger; // Тянем логгер из репозитория!

            try
            {
                DbConnection localConn = localRepository.Connection;
                if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

                string sql = syncGen.GenerateUpdateMarkAsSynced();

                // 🎯 ВОЗВРАЩАЕМ БОЕВОЙ ВАРИАНТ: Передаем оба регистра, чтобы Dapper всегда находил параметр
                await localConn.ExecuteAsync(sql, new { id, Id = id });
            }
            catch (Exception ex)
            {
                // Оставляем логирование на турникете для контроля
                logger.LogWarning($"[ОРМ МАРКЕР] [WARN] Споткнулись на фиксации IsSynced для {typeof(TEntity).Name} (ID: {id}). Текст ошибки: {ex.Message}");
            }
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

            int insertedCount = 0;
            int skippedCount = 0;
            int totalCount = 0;

            string checkLocalSql = syncGen.GenerateCountById();
            string insertLocalSql = localGen.GenerateInsert();

            using var transaction = await localConn.BeginTransactionAsync();
            try
            {
                foreach (var record in remoteRecords)
                {
                    totalCount++;
                    var exists = await localConn.ExecuteScalarAsync<bool>(checkLocalSql, new { id = record.Id }, transaction);

                    if (!exists)
                    {
                        record.IsSynced = 1; // Скачанное изначально синхронизировано

                        // Генератор сам адаптирует параметры (например, bool в 0/1 для SQLite)
                        object localParameters = localGen.PrepareParameters(record);

                        await localConn.ExecuteAsync(insertLocalSql, localParameters, transaction);
                        insertedCount++;

                        // Дебаг-лог на каждую вставку (можно поменять на LogInfo, если нужно видеть всегда)
                        logger.LogDebug($"[ОРМ МЕРЖ -> {typeof(TEntity).Name}] ➕ Вставлена новая запись. ID: {record.Id}");
                    }
                    else
                    {
                        skippedCount++;
                        // Четко пишем в дебаг, что запись уже на месте и мы её не трогали
                        logger.LogDebug($"[ОРМ МЕРЖ -> {typeof(TEntity).Name}] 💤 Запись уже существует в SQLite, пропуск. ID: {record.Id}");
                    }
                }

                await transaction.CommitAsync();

                // === ИТОГОВОЕ ЛОГИРОВАНИЕ ===
                logger.LogInfo($"[ОРМ МЕРЖ] Обработка таблицы {typeof(TEntity).Name} завершена. Всего в пакете: {totalCount} | Добавлено: {insertedCount} | Пропущено (уже есть): {skippedCount}");

                // === ПИНОК ДЛЯ АВАЛОНИИ ===
                if (insertedCount > 0)
                {
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

            return insertedCount;
        }


    }
}
