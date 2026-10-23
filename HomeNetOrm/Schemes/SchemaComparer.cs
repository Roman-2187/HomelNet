using HomeNetOrm.Models;
using System;
using System.Data;
using System.Linq;

namespace HomeNetOrm.Schemes
{
    internal class SchemaComparer
    {
        public SchemaDiff Compare(TableSchema expected, TableSchema actual)
        {
            if (string.IsNullOrEmpty(expected?.TableName) || string.IsNullOrEmpty(actual?.TableName))
                throw new ArgumentException("Имена таблиц в схемах не могут быть пустыми.");

            if (!expected.TableName.Equals(actual.TableName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Нельзя сравнивать разные таблицы: '{expected.TableName}' и '{actual.TableName}'");

            var diff = new SchemaDiff { TableName = expected.TableName };

            var actualMap = actual.Columns
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToDictionary(c => c.Name!, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var expectedCol in expected.Columns.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                if (!actualMap.Remove(expectedCol.Name!, out var actualCol))
                {
                    diff.MissingColumns.Add(expectedCol);
                    continue;
                }

                if (!AreColumnsEqual(expectedCol, actualCol))
                {
                    diff.MismatchedColumns.Add(new ColumnMismatch
                    {
                        ColumnName = expectedCol.Name,
                        Expected = expectedCol,
                        Actual = actualCol
                    });
                }
            }

            diff.ExtraColumns = actualMap.Values.ToList();

            return diff;
        }

        private bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual)
        {
            // 1. Проверяем имя (без учета регистра)
            if (!string.Equals(expected.Name, actual.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 2. Сверяем типы данных
            bool typesAreEqual = false;
            if (expected.Type == actual.Type)
            {
                typesAreEqual = true;
            }
            else
            {
                string expType = expected.Type.ToString().ToLower();
                string actType = actual.Type.ToString().ToLower();

                // БРОНЕБОЙНЫЙ СТРАЖ ЧИСЕЛ И БУЛЕЙ (Для SQLite)
                bool isExpNumericOrBool = expType.Contains("int") || expType == "boolean" || expType == "bit";
                bool isActNumericOrBool = actType.Contains("int") || actType == "boolean" || actType == "bit";

                if (isExpNumericOrBool && isActNumericOrBool)
                {
                    typesAreEqual = true;
                    actual.Type = expected.Type; // Синхронизируем для лога
                }

                // СТРАЖ СДВИГА GUID И СТРОК
                if ((expType == "varchar" || expType == "text" || expType == "string" || expType == "character varying" || expType == "guid" || expType == "uuid") &&
                    (actType == "varchar" || actType == "text" || actType == "string" || actType == "character varying" || actType == "guid" || actType == "uuid"))
                {
                    typesAreEqual = true;
                    actual.Type = expected.Type;
                }

                // Совместимость типов даты-времени
                if ((expType == "datetime" || expType == "timestamp" || expType.Contains("date")) &&
                    (actType == "datetime" || actType == "timestamp" || actType.Contains("date")))
                {
                    typesAreEqual = true;
                    actual.Type = expected.Type;
                }
            }

            if (!typesAreEqual) return false;

            // 3. Сверяем флаг первичного ключа
            if (expected.IsPrimaryKey != actual.IsPrimaryKey)
            {
                return false;
            }

            // 🎯 ФИКС: Если типы совместимы (числа/були), выравниваем флаг Nullable для SQLite,
            // чтобы разница в метаданных СУБД не плодила ложные варнинги!
            if (expected.Type == actual.Type)
            {
                actual.IsNullable = expected.IsNullable;
            }

            // 4. Сверяем дефолтные значения
            if (!AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue, expected.IsCreatedAt))
            {
                return false;
            }

            return true;
        }



        private bool AreDefaultValuesEqual(object? expected, object? actual, bool isCreatedAt)
        {
            // Приводим оба значения к чистым, нормализованным строкам
            string expStr = expected?.ToString()?.Trim() ?? string.Empty;
            string actStr = actual?.ToString()?.Trim() ?? string.Empty;

            // Очищаем от системных скобок и кавычек SQLite (например, '0', (0), ((0)))
            expStr = expStr.Trim('\'', '(', ')');
            actStr = actStr.Trim('\'', '(', ')');

            // 🎯 ЖЕЛЕЗОБЕТОННАЯ СИНХРОНИЗАЦИЯ ДЕФОЛТНЫХ НУЛЕЙ И ПУСТОТЫ:
            // Если одно значение null/empty, а второе "0" или "false" — это один и тот же дефолт!
            if (string.IsNullOrEmpty(expStr) || expStr == "0" || expStr.ToLower() == "false") expStr = "0";
            if (string.IsNullOrEmpty(actStr) || actStr == "0" || actStr.ToLower() == "false") actStr = "0";

            // Если оба значения свелись к общему дефолту (например, к "0"), они равны!
            if (expStr == actStr) return true;

            // Проверка для колонок автоматического времени создания
            if (isCreatedAt)
            {
                string lowerAct = actStr.ToLower();
                if (lowerAct.Contains("now") || lowerAct.Contains("current_timestamp")) return true;
            }

            // Если это сложные дефолты (со спецсимволами Postgres типа ::varchar), чистим хвосты
            if (expStr.Contains("::")) expStr = expStr.Split(new[] { "::" }, StringSplitOptions.None)[0].Trim('\'');
            if (actStr.Contains("::")) actStr = actStr.Split(new[] { "::" }, StringSplitOptions.None)[0].Trim('\'');

            return expStr.Equals(actStr, StringComparison.OrdinalIgnoreCase);
        }

    }
}
