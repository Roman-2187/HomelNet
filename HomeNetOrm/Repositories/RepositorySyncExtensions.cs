using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.DbInfrastructure;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// 🔥 УНИВЕРСАЛЬНЫЙ МОДУЛЬ СИНХРОНИЗАЦИИ:
    /// Расширяет внутренние репозитории ОРМ логикой пакетной Pull/Push репликации SiberNet! 🦾
    /// </summary>
    internal static class RepositorySyncExtensions
    {
      

        /// <summary>
        /// Универсальный PUSH: Находит оффлайн-изменения в локальном репозитории и выгружает их на удаленный сервер.
        /// </summary>
        public static async Task<int> PushRepositoryTableAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository,
            DbConnection remoteConn,
            ISqlGenerator<TEntity> remoteGen) where TEntity : class, ISyncableEntity
        {
            int pushedCount = 0;

            // Вытаскиваем локальные зависимости через секретный интерфейс поддержки ОРМ
            DbConnection localConn = localRepository.Connection;
            ISqlGenerator<TEntity> localGen = localRepository.SqlGenerator;

            // Гарантируем, что локальное соединение открыто перед чтением
            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();

            // Выбираем все неотправленные записи, используя абстрактный метод генератора
            var localUnsynced = await localConn.QueryAsync<TEntity>(localGen.GenerateSelectUnsynced());

            // Готовим SQL-запросы без привязки к именам конкретных СУБД
            string checkRemoteSql = remoteGen.GenerateCountById();
            string insertRemoteSql = remoteGen.GenerateInsert();
            string updateLocalSql = localGen.GenerateUpdateMarkAsSynced();

            // Обеспечиваем готовность удаленного соединения
            if (remoteConn.State != ConnectionState.Open) await remoteConn.OpenAsync();

            foreach (var record in localUnsynced)
            {
               

                // 🔥 ЧИСТОТА АРХИТЕКТУРЫ: Никакого хардкода MessageEntity! 
                // Удаленный генератор сам адаптирует параметры под свою СУБД (булевы хаки и т.д.)
                object remoteParameters = remoteGen.PrepareParameters(record);

                using var transaction = remoteConn.BeginTransaction();
                try
                {
                    // Проверяем существование записи на сервере
                    var count = await remoteConn.ExecuteScalarAsync<int>(checkRemoteSql, new { id = record.Id }, transaction);
                    if (count == 0)
                    {
                        await remoteConn.ExecuteAsync(insertRemoteSql, remoteParameters, transaction);
                        pushedCount++;
                    }

                    // Помечаем локально, что запись успешно улетела
                    await localConn.ExecuteAsync(updateLocalSql, new { id = record.Id });
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
               
            }

            return pushedCount;
        }

        /// <summary>
        /// Универсальный PULL: Скачивает живые данные с сервера и удаляет в локальной БД то, что было стерто на удаленке.
        /// </summary>
        public static async Task<int> PullRepositoryTableAsync<TEntity>(
            this ISyncSupport<TEntity> localRepository,
            DbConnection remoteConn,
            ISqlGenerator<TEntity> remoteGen) where TEntity : class, ISyncableEntity
        {
            int pulledCount = 0;

            DbConnection localConn = localRepository.Connection;
            ISqlGenerator<TEntity> localGen = localRepository.SqlGenerator;

            // Гарантируем открытие обоих соединений
            if (localConn.State != ConnectionState.Open) await localConn.OpenAsync();
            if (remoteConn.State != ConnectionState.Open) await remoteConn.OpenAsync();

            // 1. Выкачиваем актуальные записи с удаленного сервера
            var remoteRecords = await remoteConn.QueryAsync<TEntity>(remoteGen.GenerateSelectAll());
            var remoteIds = new HashSet<Guid>(remoteRecords.Select(r => r.Id));

            // 2. Выкачиваем всё из локальной таблицы
            var localRecords = await localConn.QueryAsync<TEntity>(localGen.GenerateSelectAll());

           

            string checkLocalSql = HomeNetOrm.DBProviders.Sqlite.SqliteSincExtensions.GenerateCountByIdSqlite(localGen);
            string insertLocalSql = localGen.GenerateInsert(); // Если обычный инсерт работает, оставляем. Если нет — меняем на SQLite вариант.
            string deleteLocalSql = HomeNetOrm.DBProviders.Sqlite.SqliteSincExtensions.GenerateDeleteByIdSqlite(localGen);

            // --- ШАГ А: Затягивание новых данных с сервера ---
            foreach (var record in remoteRecords)
            {
                var exists = await localConn.ExecuteScalarAsync<bool>(checkLocalSql, new { id = record.Id });
                if (!exists)
                {
                    record.IsSynced = 1; // В локальной базе она сразу считается синхронизированной

                    // Локальный генератор адаптирует параметры под свою СУБД (например, переводит bool в 0/1 для SQLite)
                    object localParameters = localGen.PrepareParameters(record);

                    await localConn.ExecuteAsync(insertLocalSql, localParameters);
                    pulledCount++;
                }
            }

            if (remoteIds.Count == 0) return pulledCount;

            // --- ШАГ Б: Очистка удаленных на сервере записей ---
            foreach (var localRecord in localRecords)
            {
                // Не трогаем локальный оффлайн, который еще даже не отправлялся на сервер
                if (localRecord.IsSynced == 0) continue;

                // Если на сервере записи больше нет — удаляем её локально
                if (!remoteIds.Contains(localRecord.Id))
                {
                    await localConn.ExecuteAsync(deleteLocalSql, new { id = localRecord.Id });
                }
            }

            return pulledCount;
        }
    }
}
