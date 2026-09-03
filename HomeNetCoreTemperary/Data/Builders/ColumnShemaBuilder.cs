using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using System;
using System.Linq.Expressions;

namespace HomeNetCoreTemperary.Data.Builders
{
    public class ColumnBuilder<TEntity> where TEntity : class
    {
        private readonly ColumnSchema _schema;

        // Билдер просто принимает готовую базовую схему из парсера!
        public ColumnBuilder(Expression<Func<TEntity, object?>> propertyExpression)
        {
            _schema = PropertySchemaParser.Parse(propertyExpression);
        }

        public ColumnBuilder<TEntity> AsPrimaryKey()
        {
            _schema.IsPrimaryKey = true;
            return this;
        }

        public ColumnBuilder<TEntity> AsAutoIncrement()
        {
            _schema.IsAutoIncrement = true;
            return this;
        }

        public ColumnBuilder<TEntity> HasLength(int length)
        {
            _schema.Length = length;
            return this;
        }

        public ColumnBuilder<TEntity> IsRequired()
        {
            _schema.IsNullable = false;
            return this;
        }

        public ColumnBuilder<TEntity> IsUnique()
        {
            _schema.IsUnique = true;
            return this;
        }

        public ColumnBuilder<TEntity> IsTrackedTimestamp()
        {
            _schema.IsCreatedAt = true;
            _schema.Type = ColumnType.DateTime;
            return this;
        }

        public ColumnBuilder<TEntity> HasDefault(object value)
        {
            _schema.DefaultValue = value;
            return this;
        }

        public ColumnBuilder<TEntity> HasForeignKey<TTarget>() where TTarget : class
        {
            _schema.IsForeignKey = true;
            _schema.ReferencedTable = typeof(TTarget).Name;
            _schema.ReferencedColumn = "id";
            return this;
        }


        public ColumnBuilder<TEntity> AsText()
        {
            _schema.Type = ColumnType.Varchar;
            _schema.Length = 8000; // С запасом под любые ссылки Яндекс.Диска и пути к файлам
            return this;
        }

        public ColumnSchema Build()
        {
            if (_schema.IsAutoIncrement && (_schema.Type != ColumnType.Integer || !_schema.IsPrimaryKey))
                throw new InvalidOperationException("AutoIncrement применим только к Integer Primary Key.");

            return _schema;
        }
    }
}
