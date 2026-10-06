using HomeNetOrm.Models;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace HomeNetOrm.Schemes
{
    public class SchemaComparer
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
            if (!string.Equals(expected.Name, actual.Name, StringComparison.OrdinalIgnoreCase) ||
                expected.IsPrimaryKey != actual.IsPrimaryKey)
            {
                return false;
            }

            bool typesAreEqual = false;
            if (expected.Type == actual.Type)
            {
                typesAreEqual = true;
            }
            else
            {
                string expType = expected.Type.ToString().ToLower();
                string actType = actual.Type.ToString().ToLower();

                // Совместимость Boolean и Integer (актуально для SQLite)
                if ((expType == "boolean" && actType == "integer") || (expType == "integer" && actType == "boolean"))
                    typesAreEqual = true;

                // 🔥 СТРАЖ СДВИГА GUID ТИПОВ:
                // Обучаем компаратор понимать, что Guid/Uuid в кодовой схеме и Varchar/Text/String/Character Varying в базах — это одно и то же!
                if ((expType == "varchar" || expType == "text" || expType == "string" || expType == "character varying" || expType == "guid" || expType == "uuid") &&
                    (actType == "varchar" || actType == "text" || actType == "string" || actType == "character varying" || actType == "guid" || actType == "uuid"))
                {
                    typesAreEqual = true;
                }

                // Совместимость типов даты-времени (DateTime, Timestamp)
                if ((expType == "datetime" || expType == "timestamp") && (actType == "datetime" || actType == "timestamp"))
                    typesAreEqual = true;
            }

            if (!typesAreEqual) return false;

            if (!AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue, expected.IsCreatedAt))
            {
                return true;
            }

            return true;
        }

        private bool AreDefaultValuesEqual(object? expected, object? actual, bool isCreatedAt)
        {
            if (expected == null && actual == null) return true;

            string expRaw = expected?.ToString() ?? string.Empty;
            string actRaw = actual?.ToString() ?? string.Empty;

            if (isCreatedAt)
            {
                string lowerAct = actRaw.ToLower();
                if (lowerAct.Contains("now") || lowerAct.Contains("current_timestamp")) return true;
            }

            if (expected == null || actual == null) return false;

            string expStr = expRaw.Trim('\'', '(', ')').Split(new[] { "::" }, StringSplitOptions.None)[0];
            string actStr = actRaw.Trim('\'', '(', ')').Split(new[] { "::" }, StringSplitOptions.None)[0];

            return expStr.Equals(actStr, StringComparison.OrdinalIgnoreCase);
        }
    }
}
