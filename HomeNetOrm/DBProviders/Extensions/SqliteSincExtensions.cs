using HomeNetOrm.Interfaces;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// 🔥 СПОЙЛЕР ДЛЯ SQLITE: Генерирует запросы синхронизации строго под синтаксис SQLite.
    /// </summary>
    public static class SqliteSincExtensions
    {
        public static string GenerateSelectUnsyncedSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            // Берем базовый селект провайдера SQLite
            string baseSelect = generator.GenerateSelectAll().TrimEnd(';');
            return $"{baseSelect} WHERE is_synced = 0 OR is_synced = '0' OR is_synced = 'false' OR is_synced IS NULL;";
        }

        public static string GenerateUpdateMarkAsSyncedSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            // SQLite работает с именами таблиц и полей напрямую
            var table = ((dynamic)generator).FormattedTable;
            string tableName = table.TableName;
            string idColumn = table.IdColumnName ?? "id";

            return $"UPDATE {tableName} SET is_synced = 1 WHERE {idColumn} = @id;";
        }

        public static string GenerateCountByIdSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var table = ((dynamic)generator).FormattedTable;
            string tableName = table.TableName;
            string idColumn = table.IdColumnName ?? "id";

            return $"SELECT COUNT(1) FROM {tableName} WHERE {idColumn} = @id;";
        }

        public static string GenerateDeleteByIdSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            return $"DELETE FROM {table.TableName} WHERE {table.IdColumnName ?? "id"} = @id;";
        }

    }
}
