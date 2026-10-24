using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    internal abstract class CoreSqlGenerator<T> where T : class
    {
        protected readonly TableSchema Schema;
        protected readonly TableSchema CamelSchema;
        protected readonly ILogger Logger;

        public IReadOnlyDictionary<string, string> DbColumns { get; }
        public IReadOnlyDictionary<string, string> DapperParams { get; }

        protected CoreSqlGenerator(IEnumerable<TableSchema> availableSchemas, ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (availableSchemas == null) throw new ArgumentNullException(nameof(availableSchemas));

            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            var foundSchema = availableSchemas.FirstOrDefault(s =>
                s != null && !string.IsNullOrEmpty(s.TableName) &&
                (s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                 s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

            if (foundSchema == null || string.IsNullOrEmpty(foundSchema.TableName))
            {
                Logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в реестре.");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует.");
            }

            var adapted = foundSchema.ToAdaptedSchemas();
            if (adapted == null || adapted.Snake == null || adapted.Camel == null ||
                adapted.Snake.Columns == null || adapted.Camel.Columns == null)
            {
                throw new InvalidOperationException($"Не удалось адаптировать схему для {targetEntityName}");
            }

            Schema = adapted.Snake;
            CamelSchema = adapted.Camel;

            var validPairs = CamelSchema.Columns
                .Zip(Schema.Columns, (camel, snake) => new { Camel = camel, Snake = snake })
                .Where(p => p.Camel != null && !string.IsNullOrEmpty(p.Camel.Name) &&
                            p.Snake != null && !string.IsNullOrEmpty(p.Snake.Name))
                .ToList();

            DbColumns = validPairs.ToDictionary(p => p.Camel.Name!, p => p.Snake.Name!, StringComparer.OrdinalIgnoreCase);
            DapperParams = validPairs.ToDictionary(p => p.Camel.Name!, p => $"@{p.Camel.Name!}", StringComparer.OrdinalIgnoreCase);
        }

        // --- 🎯 БЛАТНЫЕ ФАБРИКИ КОМАНД ДЛЯ ВЫПИЛИВАНИЯ ХАРДКОДА ---

        /// <summary>
        /// Сборка команды без параметров (SelectAll)
        /// </summary>
        protected SqlQueryCommand Command(string sql) => new(sql);

        /// <summary>
        /// Сборка команды с одним точечным параметром (Id, Email)
        /// </summary>
        protected SqlQueryCommand Command(string sql, string propertyName, object? value)
        {
            var p = new DynamicParameters();
            if (DapperParams.TryGetValue(propertyName, out var rawParam))
            {
                p.Add(rawParam.TrimStart('@'), value);
            }
            return new SqlQueryCommand(sql, p);
        }
    }
}
