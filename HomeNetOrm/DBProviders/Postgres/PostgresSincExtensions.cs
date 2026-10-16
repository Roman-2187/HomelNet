using HomeNetOrm.Interfaces.Generators;
using HomeNetCore.Models; 

namespace HomeNetOrm.Extensions
{
    public static class PostgresSincExtensions
    {
        // === НАШИ НОВЫЕ МЕТОДЫ АДАПТАЦИИ ПАРАМЕТРОВ ===

        // 1. Общий метод для всех сущностей по умолчанию (User, Friend и др.)
        public static object PrepareParametersPostgres<T>(this ISqlGenerator<T> generator, T entity) where T : class
        {
            return entity; // Возвращаем как есть
        }

        // 2. Точечный хак, который C# выберет ТОЛЬКО для MessageEntity
        public static object PrepareParametersPostgres(this ISqlGenerator<MessageEntity> generator, MessageEntity msg)
        {
            // Явно маппим свойства в типы, которые Postgres понимает без конфликтов
            return new
            {
                msg.Id,
                msg.SenderId,
                msg.ReceiverId,
                msg.Text,
                msg.MediaType,
                IsRead = msg.IsRead, // Передастся как честный bool
                msg.CreatedAt,
                IsSynced = true, // В Postgres улетает сразу как true
                msg.UpdatedAt
            };
        }

        // === ВСЕ ТВОИ ОСТАЛЬНЫЕ МЕТОДЫ ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ ===
        public static string GenerateSelectUnsyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            string baseSelect = generator.GenerateSelectAll().TrimEnd(';');
            return $"{baseSelect} WHERE is_synced = FALSE;";
        }

        public static string GenerateUpdateMarkAsSyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            string tableName = $"\"{table.TableName}\"";
            string idColumn = $"\"{table.IdColumnName ?? "id"}\"";
            return $"UPDATE {tableName} SET is_synced = TRUE WHERE {idColumn} = @id;";
        }

        public static string GenerateCountByIdPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var table = ((dynamic)generator).FormattedTable;
            string tableName = $"\"{table.TableName}\"";
            string idColumn = $"\"{table.IdColumnName ?? "id"}\"";
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
