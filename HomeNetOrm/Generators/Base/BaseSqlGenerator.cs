using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.Builders;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Base
{
    // 🔥 Единственный базовый класс в системе! Идеален для ЛЮБОГО генератора.
    internal abstract class BaseSqlGenerator<T> : ISqlGenerator<T> where T : class
    {
        protected readonly TableSchema Schema;
        protected readonly TableSchema CamelSchema;
        protected readonly ILogger Logger;

        public ISqlDialectTranslator Translator { get; }
        public IReadOnlyDictionary<string, string> DbColumns { get; }
        public IReadOnlyDictionary<string, string> DapperParams { get; }

        protected BaseSqlGenerator(IEnumerable<TableSchema> availableSchemas, ISqlDialectTranslator translator, ILogger logger)
        {
            Translator = translator ?? throw new ArgumentNullException(nameof(translator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(availableSchemas);

            // 1. Ищем и адаптируем схему из общего реестра
            var foundSchema = FindSchemaOrThrow(availableSchemas);
            var adapted = AdaptSchemaOrThrow(foundSchema);

            Schema = adapted.Snake;
            CamelSchema = adapted.Camel;

            // 2. Вытаскиваем валидные пары колонок для маппинга
            var validPairs = ExtractValidColumnPairs();

            // 3. Формируем словари для работы с Dapper и SQL-диалектами
            DbColumns = validPairs.ToDictionary(p => p.Camel.Name!, p => p.Snake.Name!, StringComparer.OrdinalIgnoreCase);
            DapperParams = validPairs.ToDictionary(p => p.Camel.Name!, p => $"@{p.Camel.Name!}", StringComparer.OrdinalIgnoreCase);
        }

        public TokenQueryBuilder<T> CreateQuery() => new(Translator, Schema, CamelSchema, DbColumns);

        #region Private Helper Methods

        private TableSchema FindSchemaOrThrow(IEnumerable<TableSchema> availableSchemas)
        {
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

            return foundSchema;
        }

        private (TableSchema Snake, TableSchema Camel) AdaptSchemaOrThrow(TableSchema foundSchema)
        {
            var adapted = foundSchema.ToAdaptedSchemas();
            if (adapted == null || adapted.Snake == null || adapted.Camel == null ||
                adapted.Snake.Columns == null || adapted.Camel.Columns == null)
            {
                throw new InvalidOperationException($"Не удалось адаптировать схему для {typeof(T).Name}");
            }

            return (adapted.Snake, adapted.Camel);
        }

        private List<DynamicColumnPair> ExtractValidColumnPairs()
        {
            return CamelSchema.Columns
                .Zip(Schema.Columns, (camel, snake) => new DynamicColumnPair { Camel = camel, Snake = snake })
                .Where(p => p.Camel != null && !string.IsNullOrEmpty(p.Camel.Name) &&
                            p.Snake != null && !string.IsNullOrEmpty(p.Snake.Name))
                .ToList();
        }

        // Вспомогательный служебный класс-контейнер для Zip-итератора
        private class DynamicColumnPair
        {
            public ColumnSchema Camel { get; init; } = null!;
            public ColumnSchema Snake { get; init; } = null!;
        }

        #endregion
    }
}
