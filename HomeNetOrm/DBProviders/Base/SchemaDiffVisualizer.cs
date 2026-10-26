using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🕵️‍♂️ Анализатор и логгер различий в схемах данных.
    /// Полностью изолирует логику форматирования текстовых предупреждений.
    /// </summary>
    internal static class SchemaDiffVisualizer
    {
        public static void LogDifferences(ILogger logger, string dbName, string camelTableName, SchemaDiff diff, List<ColumnSchema>? expectedCamelCols)
        {
            if (diff == null || diff.IsIdentical) return;

            logger.LogWarning($"[{dbName} ⚠️] ВНИМАНИЕ: Обнаружены расхождения в структуре '{camelTableName}':");

            // Локальная функция маппинга регистра
            string GetLogName(string fallbackName) =>
                expectedCamelCols?.FirstOrDefault(c => c != null && fallbackName.Equals(c.Name, StringComparison.OrdinalIgnoreCase))?.Name ?? fallbackName;

            if (diff.MissingColumns != null)
            {
                foreach (var missing in diff.MissingColumns.Where(m => m != null && !string.IsNullOrEmpty(m.Name)))
                {
                    logger.LogWarning($"   [-] ❌ Отсутствует колонка: {GetLogName(missing.Name)}");
                }
            }

            if (diff.ExtraColumns != null)
            {
                foreach (var extra in diff.ExtraColumns.Where(e => e != null && !string.IsNullOrEmpty(e.Name)))
                {
                    logger.LogWarning($"   [+] 🛑 Обнаружена лишняя колонка: {GetLogName(extra.Name)}");
                }
            }

            if (diff.MismatchedColumns != null)
            {
                var validMismatches = diff.MismatchedColumns.Where(m => m != null && !string.IsNullOrEmpty(m.ColumnName) && m.Expected != null && m.Actual != null);
                foreach (var mismatch in validMismatches)
                {
                    string reason = $"ожидалось {mismatch.Expected.Type}, прилетело {mismatch.Actual.Type}";

                    if (mismatch.Expected.IsNullable != mismatch.Actual.IsNullable)
                        reason += $" [Nullable: {mismatch.Expected.IsNullable} vs {mismatch.Actual.IsNullable}]";

                    if (mismatch.Expected.IsPrimaryKey != mismatch.Actual.IsPrimaryKey)
                        reason += $" [PK: {mismatch.Expected.IsPrimaryKey} vs {mismatch.Actual.IsPrimaryKey}]";

                    logger.LogWarning($"[{dbName}]    [*] 🔄 Расхождение в '{GetLogName(mismatch.ColumnName)}': {reason}");
                }
            }
        }
    }
}
