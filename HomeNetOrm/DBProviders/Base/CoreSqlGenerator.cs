using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🚀 ЧИСТОЕ ЯДРО: Отвечает исключительно за автопилот поиска метаданных и snake_case трансформацию.
    /// Никаких абстрактных методов, никаких интерфейсов. Чистый конструктор и FormattedTable.
    /// </summary>
    internal abstract class CoreSqlGenerator<T> where T : class
    {
        public TableSchema FormattedTable { get; }
        protected readonly ISchemaAdapter Adapter;
        protected readonly ILogger Logger;

        protected CoreSqlGenerator(ISchemaAdapter adapter, IEnumerable<TableSchema> availableSchemas, ILogger logger)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (availableSchemas == null) throw new ArgumentNullException(nameof(availableSchemas));

            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            var rawTableSchema = availableSchemas
                .FirstOrDefault(s => s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                                     s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (rawTableSchema == null || string.IsNullOrEmpty(rawTableSchema.TableName))
            {
                Logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в переданном реестре.");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует в реестре.");
            }

            FormattedTable = Adapter.ConvertToSnakeCaseSchema(rawTableSchema)
                ?? throw new InvalidOperationException($"Ошибка ОРМ-адаптера для схемы {targetEntityName}");
        }
    }
}
