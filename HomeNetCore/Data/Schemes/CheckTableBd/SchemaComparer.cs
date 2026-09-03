using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.CheckTableBd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfHomeNet.Data.Schemes.CheckTableBd
{
    public class SchemaComparer
    {
        /// <summary>
        /// Быстрое и безопасное сравнение двух схем за один проход O(N).
        /// </summary>
        public SchemaDiff Compare(TableSchema expected, TableSchema actual)
        {
            if (string.IsNullOrEmpty(expected?.TableName) || string.IsNullOrEmpty(actual?.TableName))
                throw new ArgumentException("Имена таблиц в схемах не могут быть пустыми.");

            if (!expected.TableName.Equals(actual.TableName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Нельзя сравнивать разные таблицы: '{expected.TableName}' и '{actual.TableName}'");

            var diff = new SchemaDiff { TableName = expected.TableName };

            // Переводим базу данных в словарь для мгновенного поиска за O(1)
            var actualMap = actual.Columns
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToDictionary(c => c.Name!, c => c, StringComparer.OrdinalIgnoreCase);

            // 1. Проверяем то, что ожидаем увидеть в коде (expected)
            foreach (var expectedCol in expected.Columns.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                if (!actualMap.Remove(expectedCol.Name!, out var actualCol))
                {
                    diff.MissingColumns.Add(expectedCol); // В базе колонки нет
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

            // 2. Всё, что осталось в словаре actualMap после удаления — лишнее (Extra)
            diff.ExtraColumns = actualMap.Values.ToList();

            return diff;
        }

        /// <summary>
        /// Безопасная сверка свойств колонок без выбрасывания исключений (throw)
        /// </summary>
        private bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual)
        {
            return expected.Name == actual.Name
                && expected.Type == actual.Type
                && expected.IsNullable == actual.IsNullable
                && expected.IsPrimaryKey == actual.IsPrimaryKey
                && AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue);
        }

        private bool AreDefaultValuesEqual(object? expected, object? actual)
        {
            if (expected == null && actual == null) return true;
            if (expected == null || actual == null) return false;

            // Приводим к строкам и чистим системные скобки SQLite (например, '0' или (0))
            string expStr = expected.ToString()!.Trim('\'', '(', ')');
            string actStr = actual.ToString()!.Trim('\'', '(', ')');

            return expStr.Equals(actStr, StringComparison.OrdinalIgnoreCase);
        }
    }
}





