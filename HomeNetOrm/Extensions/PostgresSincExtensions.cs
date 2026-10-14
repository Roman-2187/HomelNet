using HomeNetOrm.Interfaces.Generators;
using System;

namespace HomeNetOrm.Extensions
{
    /// <summary>
    /// 🔥 СПОЙЛЕР ДЛЯ POSTGRESQL: Генерирует запросы синхронизации строго под синтаксис Postgres.
    /// Реализовано на базе честного типа BOOLEAN для распределенной репликации! 🦾
    /// </summary>
    public static class PostgresSincExtensions
    {
        public static string GenerateSelectUnsyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            // Берем базовый селект провайдера Postgres (он уже содержит нужные кавычки "")
            string baseSelect = generator.GenerateSelectAll().TrimEnd(';');

            // 🎯 МЕНЯЕМ НА БУЛЕВУ ЛОГИКУ: В Postgres теперь честное поле boolean, числа уронят синк!
            return $"{baseSelect} WHERE is_synced = FALSE;";
        }

        public static string GenerateUpdateMarkAsSyncedPostgres<T>(this ISqlGenerator<T> generator) where T : class
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var table = ((dynamic)generator).FormattedTable;

            // В Postgres имена таблиц ОБЯЗАТЕЛЬНО должны быть обернуты в кавычки ""
            string tableName = $"\"{table.TableName}\"";
            string idColumn = $"\"{table.IdColumnName ?? "id"}\"";

            // 🎯 МЕНЯЕМ НА TRUE: Выставляем логический флаг успешной синхронизации в Postgres
            return $"UPDATE {tableName} SET is_synced = TRUE WHERE {idColumn} = @id;";
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
