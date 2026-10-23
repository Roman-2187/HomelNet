using HomeNetOrm.Models;
using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Base; 
using System.Data;

namespace HomeNetOrm.Schemes
{
    internal class SchemaComparer
    {
        // 🔥 ФИКС: Метод теперь официально принимает 3 аргумента и знает формат регистра текущей СУБД!
        public SchemaDiff Compare(TableSchema expected, TableSchema actual, NameFormat format)
        {
            if (string.IsNullOrEmpty(expected?.TableName) || string.IsNullOrEmpty(actual?.TableName))
                throw new ArgumentException("Имена таблиц в схемах не могут быть пустыми.");

            // 🎯 ФИКС: Приводим C#-имя таблицы к формату базы данных для корректной сверки имен
            string expectedTableName = expected.TableName;
            string actualAdaptedTableName = actual.TableName.ConvertTableName(format);

            if (!expectedTableName.Equals(actualAdaptedTableName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Нельзя сравнивать разные таблицы: '{expectedTableName}' и '{actualAdaptedTableName}'");

            var diff = new SchemaDiff { TableName = expected.TableName };

            // Забиваем словарь колонок, которые мы РЕАЛЬНО прочитали из физической базы данных
            var expectedMap = expected.Columns
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToDictionary(c => c.Name!, c => c, StringComparer.OrdinalIgnoreCase);

            // Идём по "чистым" C#-колонкам из твоего кода
            foreach (var actualCol in actual.Columns.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                // 🎯 ФИКС: Переводим C#-имя свойства к физическому регистру базы данных прямо на лету!
                string targetDbColumnName = actualCol.Name!.ConvertColumnName(format);

                // Ищем колонку в словаре базы данных по её адаптированному имени
                if (!expectedMap.Remove(targetDbColumnName, out var expectedCol))
                {
                    // Если в физической БД такой колонки не нашлось — записываем в missing
                    diff.MissingColumns.Add(actualCol);
                    continue;
                }

                // Если колонка есть, запускаем твою мощную логику сверки типов, Nullable и PK
                if (!AreColumnsEqual(expectedCol, actualCol, format))
                {
                    diff.MismatchedColumns.Add(new ColumnMismatch
                    {
                        ColumnName = actualCol.Name, // Выводим красивое C# имя для твоего лога
                        Expected = expectedCol,
                        Actual = actualCol
                    });
                }
            }

            // Всё, что осталось в словаре базы и не сопоставилось с C# — это лишние (Extra) колонки
            diff.ExtraColumns = expectedMap.Values.ToList();

            return diff;
        }

        private bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual, NameFormat format)
        {
            // 1. 🎯 ФИКС: Сверяем имена с учётом приведения C#-имени к регистру физической базы!
            string actualDbName = actual.Name.ConvertColumnName(format);
            if (!string.Equals(expected.Name, actualDbName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 2. Сверяем типы данных (Вся твоя бронебойная логика совместимости типов сохранена под чистую)
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

            // Если типы совместимы (числа/були), выравниваем флаг Nullable для SQLite,
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
            string expStr = expected?.ToString()?.Trim() ?? string.Empty;
            string actStr = actual?.ToString()?.Trim() ?? string.Empty;

            expStr = expStr.Trim('\'', '(', ')');
            actStr = actStr.Trim('\'', '(', ')');

            if (string.IsNullOrEmpty(expStr) || expStr == "0" || expStr.ToLower() == "false") expStr = "0";
            if (string.IsNullOrEmpty(actStr) || actStr == "0" || actStr.ToLower() == "false") actStr = "0";

            if (expStr == actStr) return true;

            if (isCreatedAt)
            {
                string lowerAct = actStr.ToLower();
                if (lowerAct.Contains("now") || lowerAct.Contains("current_timestamp")) return true;
            }

            if (expStr.Contains("::")) expStr = expStr.Split(new[] { "::" }, StringSplitOptions.None)[0].Trim('\'');
            if (actStr.Contains("::")) actStr = actStr.Split(new[] { "::" }, StringSplitOptions.None)[0].Trim('\'');

            return expStr.Equals(actStr, StringComparison.OrdinalIgnoreCase);
        }
    }
}
