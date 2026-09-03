using HomeNetCore.Enums;
using HomeNetCore.Data.Schemes;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HomeNetCoreTemperary.Data.Builders
{
    public static class PropertySchemaParser
    {
        public static ColumnSchema Parse<TEntity>(Expression<Func<TEntity, object?>> expression) where TEntity : class
        {
            MemberExpression? memberExpression = expression.Body as MemberExpression;

            // Обработка Boxing для значимых типов (int, bool, DateTime)
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

            // Чистый маппинг типов C# в твои типы ColumnType
            schema.Type = propertyInfo.PropertyType switch
            {
                Type t when t == typeof(int) || t == typeof(long) => ColumnType.Integer,
                Type t when t == typeof(string) => ColumnType.Varchar,
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => ColumnType.DateTime,
                Type t when t == typeof(bool) => ColumnType.Boolean,
                _ => ColumnType.Unspecified
            };

            return schema;
        }
    }
}

