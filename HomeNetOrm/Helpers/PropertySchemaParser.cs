using System;
using System.Linq.Expressions;
using System.Reflection;
using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.Helpers
{
    public static class PropertySchemaParser
    {
        public static ColumnSchema Parse<TEntity>(Expression<Func<TEntity, object?>> expression) where TEntity : class
        {
            MemberExpression? memberExpression = expression.Body as MemberExpression;

            // Обработка Boxing для значимых типов (int, bool, DateTime, Guid, DateTimeOffset)
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null || !(memberExpression.Member is PropertyInfo propertyInfo))
            {
                throw new ArgumentException($"Выражение должно ссылаться на свойство класса {typeof(TEntity).Name}.");
            }

            var schema = new ColumnSchema
            {
                OriginalName = propertyInfo.Name,
                Name = propertyInfo.Name // Адаптер позже переведет в snake_case
            };

            // 🔥 РАСПАКОВКА: Достаем базовый тип, если свойство является Nullable<T>
            Type propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            // Чистый маппинг типов C# в твои типы ColumnType
            schema.Type = propertyType switch
            {
                Type t when t == typeof(int) || t == typeof(long) => ColumnType.Integer, //
                Type t when t == typeof(string) => ColumnType.Varchar, //
                Type t when t == typeof(DateTime) => ColumnType.DateTime, //
                Type t when t == typeof(bool) => ColumnType.Boolean, //

                // 🎯 GUID (16-ричный код)
                Type t when t == typeof(Guid) => ColumnType.Guid,

                // ⏱ DateTimeOffset для честных штампов синхронизации
                Type t when t == typeof(DateTimeOffset) => ColumnType.DateTimeOffset,

                _ => ColumnType.Unspecified //
            };

            return schema;
        }
    }
}
