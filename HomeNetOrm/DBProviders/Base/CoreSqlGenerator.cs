using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    internal abstract class CoreSqlGenerator<T> where T : class
    {
        protected readonly TableSchema Schema;
        protected readonly ILogger Logger;

        // 🔥 НАШИ СЛОВАРИ-ТУРНИКЕТЫ: Ключ — строго имя свойства (например, "Email", "SenderId")
        protected readonly Dictionary<string, string> DbColumns = new(StringComparer.OrdinalIgnoreCase);
        protected readonly Dictionary<string, string> DapperParams = new(StringComparer.OrdinalIgnoreCase);

        protected CoreSqlGenerator(IEnumerable<TableSchema> availableSchemas, NameFormat databaseFormat, ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (availableSchemas == null) throw new ArgumentNullException(nameof(availableSchemas));

            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            var foundSchema = availableSchemas.FirstOrDefault(s =>
                s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (foundSchema == null || string.IsNullOrEmpty(foundSchema.TableName))
            {
                Logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в реестре.");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует.");
            }

            Schema = foundSchema;

            // 🎯 ЗАПОЛНЯЕМ СЛОВАРИ НА ПОЛНОМ АВТОПИЛОТЕ ПОД КОНКРЕТНУЮ СУБД:
            foreach (var col in Schema.Columns)
            {
                // Пропускаем или защищаем от пустых имен, если они вдруг затесались
                if (string.IsNullOrEmpty(col.Name)) continue;

                // 🔥 ФИКС: Ставим col.Name! чтобы убрать ворнинг CS8604
                DbColumns[col.Name!] = col.Name!.ConvertColumnName(databaseFormat);

                DapperParams[col.Name!] = $"@{col.Name!}";
            }
        }
    }
}
