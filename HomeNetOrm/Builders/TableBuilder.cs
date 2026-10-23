using System.Linq.Expressions;
using HomeNetOrm.Models;

namespace HomeNetOrm.Builders
{
    /// <summary>
    /// Билдер схемы таблицы для конкретного класса сущности.
    /// На полном автопилоте собирает структуру без ООП-каши и кэширования строк.
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

            // 🔥 ЧИСТЫЙ АВТОПИЛОТ: Схема получает только сухие данные структуры
            return new TableSchema
            {
                TableName = _tableName,
                Columns = columns,
                UseVersionTrigger = _useVersionTrigger
            };
        }
    }
}
