using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.CheckTableBd;
using System;



namespace WpfHomeNet.Data.Schemes.CheckTableBd
{
    
    public class SchemaComparer2
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
            return expected.Columns.Where
                (// Проверяем, что в actual нет колонки с таким же именем (без учёта регистра) 
                  expectedCol => !actual.Columns.Any  
                  (
                      actualCol => StringEqualsIgnoreCase(actualCol.Name,expectedCol.Name)   
                  )                                    
                )
                .ToList();                                        
        }

        /// <summary>
        /// Находит колонки, присутствующие в actual, но отсутствующие в expected.
        /// </summary>
        private List<ColumnSchema> FindExtraColumns(TableSchema expected, TableSchema actual)
        {
            return actual.Columns.Where
                (// Проверяем, что в expected нет колонки с таким же именем (без учёта регистра)   
                  actualCol =>!expected.Columns.Any                 
                  (
                    expectedCol => StringEqualsIgnoreCase(expectedCol.Name, actualCol.Name)
                  )
                )                                                                                   
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
            // Базовые проверки, которые всегда доступны
            if (expected.Name != actual.Name) return false;
            if (expected.Type != actual.Type) return false;
            if (expected.IsNullable != actual.IsNullable) return false;

            // Проверка PRIMARY KEY, так как это критично для структуры
            if (expected.IsPrimaryKey != actual.IsPrimaryKey) return false;

            // Если хотя бы одно значение не null — проверяем их оба на обязательное наличие
            if (expected.DefaultValue != null || actual.DefaultValue != null)
            {
                if (expected.DefaultValue == null || actual.DefaultValue == null)
                {
                    throw new InvalidOperationException(
                        $"Критическое расхождение: у одной из колонок ('{expected.Name}') отсутствует DefaultValue, " +
                        $"хотя у другой оно задано. Сравнение невозможно.");
                }

                // Компилятор теперь на 100% уверен, что оба значения НЕ null
                return AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue);
            }

            return true;
        }

        private bool AreDefaultValuesEqual(object expectedValue, object actualValue)
        {
            if (expectedValue == null && actualValue == null) return true;
            if (expectedValue == null || actualValue == null) return false;

            return expectedValue.Equals(actualValue);
        }

        /// <summary>
        /// Сравнивает две строки без учёта регистра (использует OrdinalIgnoreCase для точности).
        /// </summary>
        private bool StringEqualsIgnoreCase(string? a, string? b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }




    }




  
public class SchemaComparer
    {
        /// <summary>
        /// Сравнивает ожидаемую и фактическую схемы таблицы, собирая ВСЕ расхождения в один отчет.
        /// </summary>
        public SchemaDiff Compare(TableSchema expected, TableSchema actual)
        {
            // Жесткая проверка на входе, чтобы не искать ветер в поле
            if (string.IsNullOrEmpty(expected.TableName) || string.IsNullOrEmpty(actual.TableName))
            {
                throw new InvalidOperationException("Инициализация прервана: имя таблицы в схеме не может быть пустым.");
            }

            if (!string.Equals(expected.TableName, actual.TableName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Попытка сравнить разные таблицы: Ожидаемая '{expected.TableName}' против Фактической '{actual.TableName}'");
            }

            var diff = new SchemaDiff
            {
                TableName = expected.TableName,
                MissingColumns = new List<ColumnSchema>(),
                ExtraColumns = new List<ColumnSchema>(),
                MismatchedColumns = new List<ColumnMismatch>()
            };

            // Индексируем фактическую структуру из БД в Словарь для мгновенного поиска по имени
            var actualMap = actual.Columns
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToDictionary(c => c.Name!, c => c, StringComparer.OrdinalIgnoreCase);

            // Набор для отслеживания колонок из БД, которые мы успешно сопоставили с кодом
            var matchedActualNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ПРОХОД 1: Идем по нашему C#-коду (expected)
            foreach (var expectedCol in expected.Columns)
            {
                if (string.IsNullOrEmpty(expectedCol.Name))
                {
                    throw new InvalidOperationException($"В C#-конфигурации таблицы '{expected.TableName}' обнаружена колонка без имени.");
                }

                // Если в базе колонки нет — записываем в Missing
                if (!actualMap.TryGetValue(expectedCol.Name, out var actualCol))
                {
                    diff.MissingColumns.Add(expectedCol);
                    continue;
                }

                // Запоминаем, что эту колонку из БД мы проверили
                matchedActualNames.Add(actualCol.Name!);

                // Сверяем свойства. Если есть косяк — записываем в Mismatched
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

            // ПРОХОД 2: Все колонки из БД, до которых мы не дотронулись — лишние (Extra)
            foreach (var actualCol in actual.Columns)
            {
                if (string.IsNullOrEmpty(actualCol.Name))
                {
                    throw new InvalidOperationException($"В реальной БД для таблицы '{expected.TableName}' обнаружена колонка без имени.");
                }

                if (!matchedActualNames.Contains(actualCol.Name))
                {
                    diff.ExtraColumns.Add(actualCol);
                }
            }

            return diff;
        }

        private bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual)
        {
            if (expected.Name != actual.Name) return false;
            if (expected.Type != actual.Type) return false;
            if (expected.IsNullable != actual.IsNullable) return false;
            if (expected.IsPrimaryKey != actual.IsPrimaryKey) return false;

            // Наша железобетонная проверка DefaultValue
            if (expected.DefaultValue != null || actual.DefaultValue != null)
            {
                if (expected.DefaultValue == null || actual.DefaultValue == null)
                {
                    throw new InvalidOperationException(
                        $"Критическое расхождение: у колонки '{expected.Name}' на одном конце есть DefaultValue, " +
                        $"а на другом — null. Сравнение прервано.");
                }

                return expected.DefaultValue.Equals(actual.DefaultValue);
            }

            return true;
        }
    }








}





