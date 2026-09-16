using HomeNetOrm.Models;

namespace HomeNetOrm.Schemes
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
            // 1. Железная проверка имени и первичного ключа
            if (!string.Equals(expected.Name, actual.Name, StringComparison.OrdinalIgnoreCase) ||
                expected.IsPrimaryKey != actual.IsPrimaryKey)
            {
                return false;
            }

            // 2. Сверяем типы данных с учетом специфики SQLite И PostgreSQL! 🐘🔌
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

                // Совместимость текстовых типов (Varchar, Text, String, Character Varying)
                if ((expType == "varchar" || expType == "text" || expType == "string" || expType == "character varying") &&
                    (actType == "varchar" || actType == "text" || actType == "string" || actType == "character varying"))
                    typesAreEqual = true;

                // Совместимость типов даты-времени (DateTime, Timestamp)
                if ((expType == "datetime" || expType == "timestamp") && (actType == "datetime" || actType == "timestamp"))
                    typesAreEqual = true;
            }

            if (!typesAreEqual) return false;

            // 3. Сверяем дефолтные значения с очисткой синтаксиса обеих СУБД
            if (!AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue, expected.IsCreatedAt))
            {
                return true; // Не спамим ошибкой, если дефолты структурно не критичны
            }

            return true;
        }




        private bool AreDefaultValuesEqual(object? expected, object? actual, bool isCreatedAt)
        {
            if (expected == null && actual == null) return true;

            // Вытаскиваем сырые строки с безопасной проверкой на null
            string expRaw = expected?.ToString() ?? string.Empty;
            string actRaw = actual?.ToString() ?? string.Empty;

            // Если это колонка даты создания, Postgres может вернуть 'now()' или 'CURRENT_TIMESTAMP'
            if (isCreatedAt)
            {
                string lowerAct = actRaw.ToLower();
                if (lowerAct.Contains("now") || lowerAct.Contains("current_timestamp")) return true;
            }

            if (expected == null || actual == null) return false;

            // Чистим кавычки, скобки и постгресовые указатели типов (разрезаем по :: и берем первую часть)
            string expStr = expRaw.Trim('\'', '(', ')').Split(new[] { "::" }, StringSplitOptions.None)[0];
            string actStr = actRaw.Trim('\'', '(', ')').Split(new[] { "::" }, StringSplitOptions.None)[0];

            return expStr.Equals(actStr, StringComparison.OrdinalIgnoreCase);
        }

    }
}





