using HomeNetOrm.Interfaces;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// 🔥 СПОЙЛЕР ДЛЯ POSTGRESQL: Генерирует запросы синхронизации строго под синтаксис Postgres.
    /// </summary>
    public static class PostgresSincExtensions
    {
        public static string GenerateSelectUnsyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            // Берем базовый селект провайдера Postgres (он уже содержит нужные кавычки "")
            string baseSelect = generator.GenerateSelectAll().TrimEnd(';');
            return $"{baseSelect} WHERE is_synced = 0 OR is_synced = 1;"; // В Postgres булево поле честное
        }

        public static string GenerateUpdateMarkAsSyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var table = ((dynamic)generator).FormattedTable;

            // В Postgres имена таблиц ОБЯЗАТЕЛЬНО должны быть обернуты в кавычки ""
            string tableName = $"\"{table.TableName}\"";
            string idColumn = $"\"{table.IdColumnName ?? "id"}\"";

            return $"UPDATE {tableName} SET is_synced = 1 WHERE {idColumn} = @id;";
        }

        public static string GenerateCountByIdPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var table = ((dynamic)generator).FormattedTable;
            string tableName = $"\"{table.TableName}\"";
            string idColumn = $"\"{table.IdColumnName ?? "id"}\"";

            // Строгое приведение к UUID, если передается строковый Guid
            return $"SELECT COUNT(1) FROM {tableName} WHERE {idColumn}::text = @id::text;";
        }

        public static string GenerateDeleteByIdPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            return $"DELETE FROM \"{table.TableName}\" WHERE \"{table.IdColumnName ?? "id"}\" = @id;";
        }

    }
}

