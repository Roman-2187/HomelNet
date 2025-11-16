using WpfHomeNet.Data.Generators;

namespace WpfHomeNet.Data.Bilders
{
    using System;
    using WpfHomeNet.Data.Schemes;

    public enum ColumnType { Unspecified, Int, Varchar, DateTime, Boolean }

    public class ColumnBuilder
    {
        private string? _name;
        private ColumnType _type;
        private int? _length;
        private bool _isNotNull;
        private bool _isPrimaryKey;
        private bool _isUnique;
        private bool _isAutoIncrement;
        private DateTime? _createdAt;       
        private string? _comment;
        private bool _isCreatedAt;

        public ColumnBuilder(string name)
        {
            _name = name;
            _type = ColumnType.Unspecified;  // Явно задаём начальное состояние
        }


        public ColumnBuilder WithType(ColumnType type)
        {
            _type = type;
            return this;
        }

        public ColumnBuilder AsVarchar(int length)
        {
            if (length < 1 || length > 65535)
                throw new ArgumentOutOfRangeException(nameof(length),
                    "Length must be between 1 and 65535");

            _type = ColumnType.Varchar;
            _length = length;
            return this;
        }

        public ColumnBuilder AsInteger()
        {
            _type = ColumnType.Int;
            return this;
        }

        public ColumnBuilder NotNull()
        {
            _isNotNull = true;
            return this;
        }

        public ColumnBuilder PrimaryKey()
        {
            _isPrimaryKey = true;
            return this;
        }

        public ColumnBuilder Unique()
        {
            _isUnique = true;
            return this;
        }

        public ColumnBuilder AutoIncrement()
        {
            _isAutoIncrement = true;
            return this;
        }


        public ColumnBuilder DateTime()
        {
            _type = ColumnType.DateTime;
            return this;
        }


        public ColumnBuilder CreatedAt(DateTime? timestamp = null)
        {
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

        public ColumnBuilder AsDateTime()
        {
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


        public ColumnBuilder Comment(string text)
        {
            _comment = text;
            return this;
        }

        public ColumnSchema Build()
        {
            if (_isAutoIncrement && (_type != ColumnType.Int || !_isPrimaryKey))
                throw new InvalidOperationException(
                    "AutoIncrement can only be applied to Int primary keys");

            if (_type == ColumnType.Varchar && !_length.HasValue)
                throw new InvalidOperationException("Length must be specified for Varchar columns");

            return new ColumnSchema
            {
                Name = _name,
                Type = _type,
                Length = _length,
                IsNotNull = _isNotNull,
                IsPrimaryKey = _isPrimaryKey,
                IsUnique = _isUnique,
                IsAutoIncrement = _isAutoIncrement,
                CreatedAt = _createdAt,   
                IsCreatedAt = _isCreatedAt,
                Comment = _comment
            };
        }
    }

}





