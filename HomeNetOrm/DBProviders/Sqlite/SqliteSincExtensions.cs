using HomeNetOrm.Interfaces.Generators;
using HomeNetCore.Models;

namespace HomeNetOrm.DBProviders.Sqlite
{
    internal static class SqliteSincExtensions
    {
        // === МЕТОДЫ АДАПТАЦИИ ПАРАМЕТРОВ ДЛЯ SQLITE ===

        public static object PrepareParametersSqlite<T>(this ISqlGenerator<T> generator, T entity) where T : class
        {
            return entity;
        }

        public static object PrepareParametersSqlite(this ISqlGenerator<MessageEntity> generator, MessageEntity msg)
        {
            // Переводим логические флаги в числовой формат 0/1 для SQLite
            return new
            {
                msg.Id,
                msg.SenderId,
                msg.ReceiverId,
                msg.Text,
                msg.MediaType,
                IsRead = msg.IsRead ? 1 : 0,
                msg.CreatedAt,
                IsSynced = 1,
                msg.UpdatedAt
            };
        }

        // === ВСЕ ТВОИ СЛУЖЕБНЫЕ МЕТОДЫ ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ ===
        public static string GenerateSelectUnsyncedSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            string baseSelect = generator.GenerateSelectAll().TrimEnd(';');
            return $"{baseSelect} WHERE is_synced = 0 OR is_synced = '0' OR is_synced = 'false' OR is_synced IS NULL;";
        }

        public static string GenerateUpdateMarkAsSyncedSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            return $"UPDATE {table.TableName} SET is_synced = 1 WHERE {table.IdColumnName ?? "id"} = @id;";
        }

        public static string GenerateCountByIdSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            return $"SELECT COUNT(1) FROM {table.TableName} WHERE {table.IdColumnName ?? "id"} = @id;";
        }

        public static string GenerateDeleteByIdSqlite<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            return $"DELETE FROM {table.TableName} WHERE {table.IdColumnName ?? "id"} = @id;";
        }
    }
}
