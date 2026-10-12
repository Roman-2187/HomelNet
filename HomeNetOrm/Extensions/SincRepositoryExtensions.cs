using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Extensions
{
    /// <summary>
    /// 🔥 СПОЙЛЕР ДЛЯ РЕПОЗИТОРИЕВ ЧЕРЕЗ МЕТОДЫ РАСШИРЕНИЯ:
    /// Навешивает логику пакетной Pull/Push синхронизации SiberNet прямо на чистые DbConnection!
    /// </summary>
    public static class SincRepositoryExtensions
    {
        // 🔒 Статический семафор для жесткой блокировки параллельных потоков в Postgres
        private static readonly SemaphoreSlim _pgDbLock = new SemaphoreSlim(1, 1);



        /// <summary>
        /// Универсальный PULL: Скачивает данные и возвращает количество добавленных записей.
        /// </summary>
        public static async Task<int> PullTableAsync<TEntity>(
            this DbConnection sqlite,
            DbConnection pg,
            ISqlGenerator<TEntity> sqliteGen,
            ISqlGenerator<TEntity> pgGen) where TEntity : class, ISyncableEntity
        {
            int pulledCount = 0;

            // 1. Выкачиваем живые записи с Postgres
            var pgRecords = await pg.QueryAsync<TEntity>(pgGen.GenerateSelectAll());
            var remoteIds = new System.Collections.Generic.HashSet<Guid>();
            foreach (var record in pgRecords) remoteIds.Add(record.Id);

            var localRecords = await sqlite.QueryAsync<TEntity>(sqliteGen.GenerateSelectAll());

            string checkSql = sqliteGen.GenerateCountByIdSqlite();
            string insertSql = sqliteGen.GenerateInsert();
            string deleteSql = sqliteGen.GenerateDeleteByIdSqlite();

            // --- ШАГ А: Вставка новых данных с сервера ---
            foreach (var record in pgRecords)
            {
                var exists = await sqlite.ExecuteScalarAsync<bool>(checkSql, new { id = record.Id });
                if (!exists)
                {
                    record.IsSynced = 1;
                    await sqlite.ExecuteAsync(insertSql, record);
                    pulledCount++; // Считаем, сколько затянули
                }
            }

            if (remoteIds.Count == 0) return pulledCount;

            // --- ШАГ Б: ОЧИСТКА УДАРЕННЫХ НА СЕРВЕРЕ ЗАПИСЕЙ ---
            foreach (var localRecord in localRecords)
            {
                if (localRecord.IsSynced == 0) continue;

                if (!remoteIds.Contains(localRecord.Id))
                {
                    await sqlite.ExecuteAsync(deleteSql, new { id = localRecord.Id });
                }
            }

            return pulledCount;
        }

        /// <summary>
        /// Универсальный PUSH: Находит оффлайн-изменения и возвращает количество выгруженных строк.
        /// </summary>
        public static async Task<int> PushTableAsync<TEntity>(
            this DbConnection sqlite,
            DbConnection pg,
            ISqlGenerator<TEntity> sqliteGen,
            ISqlGenerator<TEntity> pgGen) where TEntity : class, ISyncableEntity
        {
            int pushedCount = 0;

            var localUnsynced = await sqlite.QueryAsync<TEntity>(sqliteGen.GenerateSelectUnsyncedSqlite());

            string checkPgSql = pgGen.GenerateCountByIdPostgres();
            string insertPgSql = pgGen.GenerateInsert();
            string updateSql = sqliteGen.GenerateUpdateMarkAsSyncedSqlite();

            foreach (var record in localUnsynced)
            {
                await _pgDbLock.WaitAsync();
                using var transaction = pg.BeginTransaction();
                try
                {
                    var count = await pg.ExecuteScalarAsync<int>(checkPgSql, new { id = record.Id }, transaction);
                    if (count == 0)
                    {
                        await pg.ExecuteAsync(insertPgSql, record, transaction);
                        pushedCount++; // Считаем успешный пуш
                    }

                    await sqlite.ExecuteAsync(updateSql, new { id = record.Id });
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    _pgDbLock.Release();
                }
            }

            return pushedCount;
        }

    }
}
