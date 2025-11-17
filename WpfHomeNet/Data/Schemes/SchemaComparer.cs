namespace WpfHomeNet.Data.Schemes
{
    using System;

    public class SchemaComparer
    {
        /// <summary>
        /// Сравнивает ожидаемую и фактическую схемы таблицы, возвращая различия.
        /// </summary>
        /// <param name="expected">Ожидаемая схема таблицы (эталон)</param>
        /// <param name="actual">Фактическая схема таблицы (из БД)</param>
        /// <returns>Объект с перечнем отсутствующих, лишних и несовпадающих колонок</returns>
        public SchemaDiff Compare(TableSchema expected, TableSchema actual)
        {
            var diff = new SchemaDiff { TableName = expected.TableName };

            // Находим колонки, которые есть в эталонной схеме, но отсутствуют в реальной
            diff.MissingColumns = FindMissingColumns(expected, actual);

            // Находим колонки, которые есть в реальной схеме, но отсутствуют в эталонной
            diff.ExtraColumns = FindExtraColumns(expected, actual);

            // Находим колонки с совпадающими именами, но разными свойствами
            diff.MismatchedColumns = FindMismatchedColumns(expected, actual);

            return diff;
        }

        /// <summary>
        /// Находит колонки, присутствующие в expected, но отсутствующие в actual.
        /// </summary>
        private List<ColumnSchema> FindMissingColumns(TableSchema expected, TableSchema actual)
        {
            return expected.Columns
                .Where(expectedCol =>
                    // Проверяем, что в actual нет колонки с таким же именем (без учёта регистра)
                    !actual.Columns.Any(actualCol =>
                        StringEqualsIgnoreCase(actualCol.Name, expectedCol.Name)))
                .ToList();
        }

        /// <summary>
        /// Находит колонки, присутствующие в actual, но отсутствующие в expected.
        /// </summary>
        private List<ColumnSchema> FindExtraColumns(TableSchema expected, TableSchema actual)
        {
            return actual.Columns
                .Where(actualCol =>
                    // Проверяем, что в expected нет колонки с таким же именем (без учёта регистра)
                    !expected.Columns.Any(expectedCol =>
                        StringEqualsIgnoreCase(expectedCol.Name, actualCol.Name)))
                .ToList();
        }

        /// <summary>
        /// Находит колонки с одинаковыми именами, но различающимися свойствами.
        /// </summary>
        private List<ColumnMismatch> FindMismatchedColumns(TableSchema expected, TableSchema actual)
        {
            var mismatches = new List<ColumnMismatch>();

            foreach (var expectedCol in expected.Columns)
            {
                // Ищем колонку в actual с таким же именем (без учёта регистра)
                var actualCol = actual.Columns
                    .FirstOrDefault(col => StringEqualsIgnoreCase(col.Name, expectedCol.Name));


                // Если колонка найдена, но её свойства не совпадают — добавляем в несоответствия
                if (actualCol != null && !AreColumnsEqual(expectedCol, actualCol))
                {
                    mismatches.Add(new ColumnMismatch
                    {
                        ColumnName = expectedCol.Name,
                        Expected = expectedCol,
                        Actual = actualCol
                    });
                }
            }

            return mismatches;
        }

        /// <summary>
        /// Проверяет, полностью ли совпадают свойства двух колонок.
        /// </summary>
        private bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual)
        {
            return expected.Type == actual.Type &&
                   expected.Length == actual.Length &&
                   expected.IsNullable == actual.IsNullable &&
                   expected.IsPrimaryKey == actual.IsPrimaryKey &&
                   expected.IsUnique == actual.IsUnique &&
                   // Особое сравнение для значения по умолчанию (может быть null или разным типом)
                   AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue) &&
                   expected.IsAutoIncrement == actual.IsAutoIncrement;
        }

        /// <summary>
        /// Сравнивает значения по умолчанию с учётом возможных null и разных типов.
        /// </summary>
        private bool AreDefaultValuesEqual(object expected, object actual)
        {
            // Оба значения null — считаем равными
            if (expected == null && actual == null)
                return true;

            // Одно null, другое нет — не равны
            if (expected == null || actual == null)
                return false;

            // Оба не null — используем стандартный метод Equals для сравнения значений
            return expected.Equals(actual);
        }

        /// <summary>
        /// Сравнивает две строки без учёта регистра (использует OrdinalIgnoreCase для точности).
        /// </summary>
        private bool StringEqualsIgnoreCase(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }












}





