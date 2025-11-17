using WpfHomeNet.Data.Generators;

namespace WpfHomeNet.Data.Bilders
{
    using System;
    using System.Data.Common;
    using WpfHomeNet.Data.Schemes;

    public enum ColumnType { Unspecified, Integer, Varchar, DateTime, Boolean }

    public class ColumnBuilder
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
            _type = ColumnType.Integer;
            return this;
        }
        public ColumnBuilder DateTime()
        {
            _type = ColumnType.DateTime;
            return this;
        }

        public ColumnBuilder AllowNull()
        {
            _isNullable = true;
            return this;
        }


        public ColumnBuilder DisallowNull()
        {
            _isNullable = false;
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


        public ColumnBuilder DefaultValue(string value)
        { 
            _defaultValue = value;
            return this;
        }

        public ColumnBuilder Comment(string text)
        {
            _comment = text;
            return this;
        }

        public ColumnSchema Build()
        {
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



    class Myclass
    {
        public TableSchema ReadSchemaFromDb(string tableName, DbConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT
            column_name,
            data_type,
            character_maximum_length,
            is_nullable,
            column_default,
            (
                SELECT 't'
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                WHERE tc.table_name = @tableName
                    AND tc.constraint_type = 'PRIMARY KEY'
                    AND kcu.column_name = c.column_name
            ) AS is_primary_key
        FROM information_schema.columns c
        WHERE table_name = @tableName
        ORDER BY ordinal_position";

            cmd.Parameters.AddWithValue("@tableName", tableName);

            using var reader = cmd.ExecuteReader();
            var columns = new List<ColumnSchema>();

            while (reader.Read())
            {
                var col = new ColumnSchema
                {
                    Name = reader["column_name"].ToString(),
                    Type = MapDbTypeToDotNetType(reader["data_type"].ToString()),
                    Length = reader["character_maximum_length"] as int?,
                    IsNullable = reader["is_nullable"].ToString() == "YES",
                    IsPrimaryKey = reader["is_primary_key"]?.ToString() == "t",
                    IsAutoIncrement = IsAutoIncrement(reader["column_name"].ToString(), connection), // Отдельный метод
                    DefaultValue = reader["column_default"]
                };
                columns.Add(col);
            }

            return new TableSchema
            {
                TableName = tableName,
                Columns = columns
            };
        }
    }



   

   


  

}





