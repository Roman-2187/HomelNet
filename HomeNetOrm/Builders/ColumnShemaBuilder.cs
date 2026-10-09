using HomeNetOrm.Enums;
using HomeNetOrm.Models;
using System.Linq.Expressions;
using HomeNetOrm.DBProviders.Extensions;

namespace HomeNetOrm.Builders
{
    public class ColumnBuilder<TEntity> where TEntity : class
    {
        private readonly ColumnSchema _schema;

        public ColumnBuilder(Expression<Func<TEntity, object?>> propertyExpression)
        {
            // 🎯 Инициализируем схему через наш маппер расширений
            _schema = propertyExpression.MapPropertySchema();

            // 🧙‍♂️ СБРОС АВТОИНКРЕМЕНТА:
            // По умолчанию выключен. Включается только явным вызовом .AsAutoIncrement()
            _schema.IsAutoIncrement = false;
        }

        public ColumnBuilder<TEntity> AsPrimaryKey()
        {
            _schema.IsPrimaryKey = true;
            return this;
        }

        // 🔥 Явно размечает колонку в схеме как GUID/UUID
        public ColumnBuilder<TEntity> AsGuid()
        {
            _schema.Type = ColumnType.Guid;
            return this;
        }

        public ColumnBuilder<TEntity> AsAutoIncrement()
        {
            _schema.IsAutoIncrement = true;
            _schema.IsPrimaryKey = true; // Автоинкремент без ключа быть не может
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

        // 🎯 ИСПРАВЛЕНО: Полностью перешли на DateTimeOffset для фикса расхождений в логах
        public ColumnBuilder<TEntity> IsTrackedTimestamp()
        {
            _schema.IsCreatedAt = true;
            _schema.Type = ColumnType.DateTimeOffset;
            return this;
        }

        // 🎯 ИСПРАВЛЕНО: Синхронно с датой создания использует высокоточный DateTimeOffset
        public ColumnBuilder<TEntity> IsUpdatedAtTimestamp()
        {
            _schema.Type = ColumnType.DateTimeOffset;
            return this;
        }

        public ColumnBuilder<TEntity> HasDefault(object value, ColumnType? forceType = null)
        {
            _schema.DefaultValue = value;
            if (forceType != null)
            {
                _schema.Type = forceType.Value;
            }
            return this;
        }

        public ColumnBuilder<TEntity> HasForeignKey<TTarget>() where TTarget : class
        {
            _schema.IsForeignKey = true;
            _schema.ReferencedTable = typeof(TTarget).Name;
            _schema.ReferencedColumn = "id";
            return this;
        }

        public ColumnBuilder<TEntity> AsText(ColumnType? customType = null)
        {
            _schema.Type = customType ?? ColumnType.Varchar;
            _schema.Length = 8000;
            return this;
        }

        public ColumnSchema Build()
        {
            // 🛠 ВАЛИДАЦИЯ АВТОИНКРЕМЕНТА: 
            // Проверяем тип Integer только если автоинкремент был включен вручную.
            if (_schema.IsAutoIncrement)
            {
                if (_schema.Type != ColumnType.Integer || !_schema.IsPrimaryKey)
                {
                    throw new InvalidOperationException($"Ошибка в колонке {_schema.Name}: AutoIncrement применим только к Integer Primary Key.");
                }
            }

            return _schema;
        }
    }
}
