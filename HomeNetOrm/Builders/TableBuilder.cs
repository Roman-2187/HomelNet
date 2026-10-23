using System.Linq.Expressions;
using HomeNetOrm.Models;

namespace HomeNetOrm.Builders
{
    /// <summary>
    /// Билдер схемы таблицы для конкретного класса сущности
    /// </summary>
    public class TableBuilder<TEntity> where TEntity : class
    {
        private readonly string _tableName;
        private readonly List<ColumnBuilder<TEntity>> _columnBuilders = new();
        private bool _useVersionTrigger = false;

        public TableBuilder(string tableName) => _tableName = tableName;

        /// <summary>
        /// Принимает C#-выражение свойства модели (например: x => x.DeviceId)
        /// </summary>
        public ColumnBuilder<TEntity> AddColumn(Expression<Func<TEntity, object?>> propertyExpression)
        {
            var builder = new ColumnBuilder<TEntity>(propertyExpression);
            _columnBuilders.Add(builder);
            return builder;
        }

        /// <summary>
        /// Включает для таблицы автоматическое версионирование через Postgres-триггер
        /// </summary>
        public TableBuilder<TEntity> WithVersionTracking()
        {
            _useVersionTrigger = true;
            return this;
        }

        /// <summary>
        /// Финализирует сборку схемы таблицы
        /// </summary>
        public TableSchema Generate()
        {
            var columns = _columnBuilders
                .Select(builder => builder.Build())
                .ToList();

            var primaryKeysCount = columns.Count(c => c.IsPrimaryKey);

            var schema = new TableSchema
            {
                TableName = _tableName,
                Columns = columns,
                UseVersionTrigger = _useVersionTrigger // Пробрасываем флаг триггера в схему
            };

            // Запускаем сборку строк AllFields, SetClause, InsertFields
            schema.Initialize();

            // 🛡️ Защита от составного ключа:
            // Если ключ один — пишем его имя. Если ключей больше (как в DeviceSync) — ставим null.
            schema.IdColumnName = primaryKeysCount == 1
                ? columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name
                : null;

            return schema;
        }
    }
}
