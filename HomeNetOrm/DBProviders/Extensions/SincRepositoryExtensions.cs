using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Extensions
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
        /// Универсальный PULL: Выкачивает все записи с сервера (Postgres), 
        /// дописывает отсутствующие и УДАЛЯЕТ те, что были стерты на сервере.
        /// </summary>
        public static async Task PullTableAsync<TEntity>(
            this DbConnection sqlite,
            DbConnection pg,
            ISqlGenerator<TEntity> sqliteGen,
            ISqlGenerator<TEntity> pgGen) where TEntity : class, ISyncableEntity
        {
            // 1. Выкачиваем живые записи с Postgres
            var pgRecords = await pg.QueryAsync<TEntity>(pgGen.GenerateSelectAll());

            // Загоняем все серверные ID в быстрый хэш-сет для моментального поиска в памяти
            var remoteIds = new System.Collections.Generic.HashSet<Guid>();
            foreach (var record in pgRecords)
            {
                remoteIds.Add(record.Id);
            }

            // 2. Достаем все локальные записи из SQLite для сверки хвостов
            var localRecords = await sqlite.QueryAsync<TEntity>(sqliteGen.GenerateSelectAll());

            string checkSql = sqliteGen.GenerateCountByIdSqlite();
            string insertSql = sqliteGen.GenerateInsert();
            string deleteSql = sqliteGen.GenerateDeleteByIdSqlite(); // 🔥 Наш новый SQLite-экстеншен удаления!

            // --- ШАГ А: Вставка новых данных с сервера ---
            foreach (var record in pgRecords)
            {
                var exists = await sqlite.ExecuteScalarAsync<bool>(checkSql, new { id = record.Id });
                if (!exists)
                {
                    record.IsSynced = 1;
                    await sqlite.ExecuteAsync(insertSql, record);
                }
            }

            // --- ШАГ Б: ОЧИСТКА УДАРЕННЫХ НА СЕРВЕРЕ ЗАПИСЕЙ ---
            foreach (var localRecord in localRecords)
            {
                // 🔥 ЗАЩИТА ОФФЛАЙН-ДАННЫХ: 
                // Если запись локально еще НЕ СИНХРОНИЗИРОВАНА (is_synced = 0),
                // значит это новый клиент, созданный в оффлайне. Пропускаем его, не стираем!
                if (localRecord.IsSynced == 0)
                {
                    continue;
                }

                // А вот если запись уже была синхронизирована ранее (IsSynced == 1),
                // но теперь на сервере в Postgres её нет — значит это реальное удаление!
                if (!remoteIds.Contains(localRecord.Id))
                {
                    // Теперь безопасно выпиливаем её из локальной базы
                    await sqlite.ExecuteAsync(deleteSql, new { id = localRecord.Id });
                }
            }

        }




        /// <summary>
        /// Универсальный PUSH: Находит все оффлайн-изменения в SQLite (is_synced = 0) 
        /// и пачкой выгружает их на сервер Postgres, защищая от гонки потоков.
        /// </summary>
        public static async Task PushTableAsync<TEntity>(
            this DbConnection sqlite,
            DbConnection pg,
            ISqlGenerator<TEntity> sqliteGen,
            ISqlGenerator<TEntity> pgGen) where TEntity : class, ISyncableEntity
        {
            // 1. 🔥 ИСПОЛЬЗУЕМ КЛАССЫ РАСШИРЕНИЯ: Достаем оффлайн-данные из SQLite по правилам SQLite
            var localUnsynced = await sqlite.QueryAsync<TEntity>(sqliteGen.GenerateSelectUnsyncedSqlite());

            // 2. 🔥 ИСПОЛЬЗУЕМ КЛАССЫ РАСШИРЕНИЯ: Готовим строго разделенные SQL-команды под каждую СУБД
            string checkPgSql = pgGen.GenerateCountByIdPostgres(); // Postgres-специфика проверки
            string insertPgSql = pgGen.GenerateInsert();          // Базовый метод вставки ОРМ
            string updateSql = sqliteGen.GenerateUpdateMarkAsSyncedSqlite(); // SQLite-специфика сброса флага

            foreach (var record in localUnsynced)
            {
                // 🔒 Выстраиваем потоки в очередь перед отправкой в Postgres
                await _pgDbLock.WaitAsync();
                using var transaction = pg.BeginTransaction();
                try
                {
                    // Проверяем, нет ли записи на сервере Postgres в контексте транзакции
                    var count = await pg.ExecuteScalarAsync<int>(checkPgSql, new { id = record.Id }, transaction);
                    if (count == 0)
                    {
                        await pg.ExecuteAsync(insertPgSql, record, transaction);
                    }

                    // Сбрасываем флаг синхронизации локально в SQLite
                    await sqlite.ExecuteAsync(updateSql, new { id = record.Id });

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw; // Пробрасываем ошибку выше, чтобы координатор знал о сбое
                }
                finally
                {
                    _pgDbLock.Release(); // Освобождаем дорогу следующей записи
                }
            }
        }
    }
}
