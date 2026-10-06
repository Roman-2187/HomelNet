using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetOrm.Models;
using System.Linq.Expressions;

namespace HomeNetOrm.Builders
{
    public class ColumnBuilder<TEntity> where TEntity : class
    {
        private readonly ColumnSchema _schema;

        public ColumnBuilder(Expression<Func<TEntity, object?>> propertyExpression)
        {
            _schema = PropertySchemaParser.Parse(propertyExpression);

            // 🧙‍♂️ СБРОС СЛИШКОМ УМНОГО ПАРСЕРА:
            // Изначально выключаем AutoIncrement. Он включится ТОЛЬКО если 
            // разработчик сам явно вызовет метод .AsAutoIncrement() в SchemaRegistry!
            _schema.IsAutoIncrement = false;
        }

        public ColumnBuilder<TEntity> AsPrimaryKey()
        {
            _schema.IsPrimaryKey = true;
            return this;
        }

        // 🔥 НОВЫЙ МЕТОД: Явно размечает колонку в схеме как GUID/UUID
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

        public ColumnBuilder<TEntity> IsTrackedTimestamp()
        {
            _schema.IsCreatedAt = true;
            _schema.Type = ColumnType.DateTime;
            return this;
        }

        // 🔥 НОВЫЙ МЕТОД: Для временной метки обновления (паттерн Last Write Wins)
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

        // Если вызван без параметров — это обычный длинный текст. 
        // Если передать ColumnType — принудительно перезапишет тип на нужный.
        public ColumnBuilder<TEntity> AsText(ColumnType? customType = null)
        {
            // Если в твоем enum есть ColumnType.Text, используем его, иначе оставляем Varchar
            _schema.Type = customType ?? ColumnType.Varchar;
            _schema.Length = 8000;
            return this;
        }

        public ColumnSchema Build()
        {
            // 🛠 ФИКС ВАЛИДАЦИИ: 
            // Теперь проверяем тип Integer только ЕСЛИ разработчик сам явно вызвал .AsAutoIncrement().
            // Для наших Guid ключей IsAutoIncrement будет равен false, и эта проверка благополучно пропустится!
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
