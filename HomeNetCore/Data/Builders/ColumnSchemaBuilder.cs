using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;


namespace WpfHomeNet.Data.Builders
{
    

    public class ColumnSchemaBuilder
    {
        private string? _name;
        private ColumnType _type;
        private int? _length;
        private bool _isNullable;
        private bool _isPrimaryKey;
        private bool _isUnique;
        private bool _isAutoIncrement;
        private DateTime? _createdAt;       
        private string? _comment;
        private bool _isCreatedAt;
        private object? _defaultValue;

        public ColumnSchemaBuilder(string name)
        {
            _name = name;
            _type = ColumnType.Unspecified;  // Явно задаём начальное состояние
        }


        public ColumnSchemaBuilder WithType(ColumnType type)
        {
            if (type == ColumnType.Unknown)
                throw new ArgumentException(
                    "Cannot use ColumnType.Unknown — specify a concrete type.",
                    nameof(type));

            _type = type;
            return this;
        }

        public ColumnSchemaBuilder AsVarchar(int length)
        {
            if (length < 1 || length > 65535)
                throw new ArgumentOutOfRangeException(nameof(length),
                    "Length must be between 1 and 65535");

            _type = ColumnType.Varchar;
            _length = length;
            return this;
        }

        public ColumnSchemaBuilder AsInteger()
        {
            _type = ColumnType.Integer;
            return this;
        }
        public ColumnSchemaBuilder DateTime()
        {
            _type = ColumnType.DateTime;
            return this;
        }

        public ColumnSchemaBuilder AllowNull()
        {
            _isNullable = true;
            return this;
        }


        public ColumnSchemaBuilder DisallowNull()
        {
            _isNullable = false;
            return this;
        }

        public ColumnSchemaBuilder PrimaryKey()
        {
            _isPrimaryKey = true;
            return this;
        }

        public ColumnSchemaBuilder Unique()
        {
            _isUnique = true;
            return this;
        }

        public ColumnSchemaBuilder AutoIncrement()
        {
            _isAutoIncrement = true;
            return this;
        }


       
        public ColumnSchemaBuilder CreatedAt(DateTime? timestamp = null)
        {
            if (_type == ColumnType.Unknown)
                throw new InvalidOperationException(
                    "Cannot set CreatedAt for column with Unknown type.");

            if (_type == ColumnType.Unspecified)
            {
                _type = ColumnType.DateTime;  // Если тип не задан — устанавливаем
            }
            else if (_type != ColumnType.DateTime)
            {
                throw new InvalidOperationException(
                    $"CreatedAt() requires DateTime type, but got {_type}.");
            }

            _createdAt = timestamp ?? System.DateTime.UtcNow;
            return this;
        }

        public ColumnSchemaBuilder AsDateTime()
        {
            if (_type == ColumnType.Unknown)
                throw new InvalidOperationException(
                    "Cannot call AsDateTime() when type is Unknown.");

            if (_type == ColumnType.Unspecified)
            {
                _type = ColumnType.DateTime;  // Авто установка типа
            }
            else if (_type != ColumnType.DateTime)
            {
                throw new InvalidOperationException("Для CreatedAt нужен DateTime!");
            }

            _isCreatedAt = true;  // Флаг: эта колонка — «время создания»
            return this;
        }


        public ColumnSchemaBuilder DefaultValue(string value)
        { 
            _defaultValue = value;
            return this;
        }

        public ColumnSchemaBuilder Comment(string text)
        {
            _comment = text;
            return this;
        }

        public ColumnSchema Build()
        {
            // Проверяем, что тип задан и не является Unknown
            if (_type == ColumnType.Unspecified || _type == ColumnType.Unknown)
                throw new InvalidOperationException(
                    $"Column '{_name}' must have a specified type (got: {_type}).");

            if (_isAutoIncrement && (_type != ColumnType.Integer || !_isPrimaryKey))
                throw new InvalidOperationException(
                    "AutoIncrement can only be applied to Int primary keys");

            if (_type == ColumnType.Varchar && !_length.HasValue)
                throw new InvalidOperationException("Length must be specified for Varchar columns");

            return new ColumnSchema
            {
                Name = _name,
                Type = _type,
                Length = _length,
                IsNullable = _isNullable,
                IsPrimaryKey = _isPrimaryKey,
                IsUnique = _isUnique,
                IsAutoIncrement = _isAutoIncrement,
                CreatedAt = _createdAt,   
                IsCreatedAt = _isCreatedAt,
                DefaultValue = _defaultValue,
                Comment = _comment
            };
        }
    }

}





