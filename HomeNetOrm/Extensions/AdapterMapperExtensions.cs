using System.Linq.Expressions;
using System.Reflection;
using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// Статический движок маппинга: расширяет функционал адаптера схем, 
    /// выковыривая и сопоставляя C# типы свойств с внутренними типами СУБД.
    /// </summary>
    public static class AdapterMapperExtensions
    {
        /// <summary>
        /// Разбирает C# Lambda-выражение свойства и маппит его во внутренний ColumnSchema со строгим типом ORM.
        /// </summary>
        public static ColumnSchema MapPropertySchema<TEntity>(this Expression<Func<TEntity, object?>> expression) where TEntity : class
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            MemberExpression? memberExpression = expression.Body as MemberExpression;

            // Снимаем Boxing-обертку для значимых типов (int, bool, DateTime, Guid, Enums)
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null || !(memberExpression.Member is PropertyInfo propertyInfo))
            {
                throw new ArgumentException($"Выражение должно ссылаться на валидное свойство класса {typeof(TEntity).Name}.");
            }

            var schema = new ColumnSchema
            {
                OriginalName = propertyInfo.Name,
                Name = propertyInfo.Name // Адаптер в слое конвертации позже переведет это в snake_case 🐍
            };

            // Распаковываем базовый тип, если свойство обернуто в Nullable<T>
            Type propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            // 🔥 АВТОМАТИЗАЦИЯ ЕНАМОВ: Если свойство является Enum — сохраняем его в БД как Integer (число)
            if (propertyType.IsEnum)
            {
                schema.Type = ColumnType.Integer;
                return schema;
            }

            // Прямое сопоставление фундаментальных типов C# и ColumnType движка
            schema.Type = propertyType switch
            {
                Type t when t == typeof(int) || t == typeof(long) => ColumnType.Integer,
                Type t when t == typeof(string) => ColumnType.Varchar,
                Type t when t == typeof(bool) => ColumnType.Boolean,

                // Наш бро для кросс-базовой синхронизации (SQLite + Postgres) ⚡
                Type t when t == typeof(Guid) => ColumnType.Guid,

                // ⏱ ТОТАЛЬНЫЙ DateTimeOffset: 
                // Заставляем любые типы даты-времени C# жестко маппиться в тип со смещением!
                Type t when t == typeof(DateTime) => ColumnType.DateTimeOffset,
                Type t when t == typeof(DateTimeOffset) => ColumnType.DateTimeOffset,

                _ => ColumnType.Unspecified
            };

            return schema;
        }
    }
}
