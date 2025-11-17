using System.Data.Common;
using WpfHomeNet.Data.Bilders;
using WpfHomeNet.Data.Schemes;

namespace WpfHomeNet.Data.Generators
{
    public class SqliteQueryGenerator 
    {
        
    }



    public interface ISchemaProvider
    {
        Task<List<ColumnSchema>> GetActualColumnsAsync(
            DbConnection connection,
            string tableName);
    }




    public abstract class ExpectedSchemaBase
    {
        public string? TableName { get; protected set; }

        protected readonly List<ColumnSchema> _columns = new();

        protected void AddColumn(ColumnSchema column)
        {
            _columns.Add(column);
        }

        public List<ColumnSchema> GetExpectedColumns()
        {
            return _columns;
        }

        // Шаблонный метод для построения схемы — должен быть реализован в наследниках
        protected abstract void BuildSchema();
    }


    public class UsersExpectedSchema : ExpectedSchemaBase
    {
        public UsersExpectedSchema()
        {
            TableName = "users";
            BuildSchema();
        }

        protected override void BuildSchema()
        {
            AddColumn(new ColumnSchema
            {
                Name = "Id",
                Type = ColumnType.Integer,
                IsPrimaryKey = true,
                IsAutoIncrement = true
            });

            AddColumn(new ColumnSchema
            {
                Name = "FirstName",
                Type = ColumnType.Varchar,
                Length = 50,
                IsNullable = false
            });

            AddColumn(new ColumnSchema
            {
                Name = "LastName",
                Type = ColumnType.Varchar,
                Length = 50,
                IsNullable = true
            });

            AddColumn(new ColumnSchema
            {
                Name = "Email",
                Type = ColumnType.Varchar,
                Length = 50,
                IsNullable = false,
                IsUnique = true
            });

            // ... остальные колонки
        }
    }



    public class PostgresSchemaProvider : ISchemaProvider
    {
        public async Task<List<ColumnSchema>> GetActualColumnsAsync(
            DbConnection connection, string tableName)
        {
            var columns = new List<ColumnSchema>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                column_name,
                data_type,
                character_maximum_length,
                is_nullable,
                column_key,
                extra
            FROM information_schema.columns
            WHERE table_name = @tableName";

            command.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnSchema
                {
                    Name = reader.GetString(0),
                    Type = ParsePostgresType(reader.GetString(1)),
                    Length = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    IsNullable = reader.GetString(3) == "YES",
                    IsPrimaryKey = reader.GetString(4) == "PRI",
                    IsAutoIncrement = reader.GetString(5) == "auto_increment"
                });
            }

            return columns;
        }

        private ColumnType ParsePostgresType(string pgType)
        {
            switch (pgType.ToLower())
            {
                case "integer": return ColumnType.Integer;
                case "character varying": return ColumnType.Varchar;
                // ... другие типы
                default: return ColumnType.Unknown;
            }
        }
    }

    public class SqliteSchemaProvider : ISchemaProvider
    {
        public async Task<List<ColumnSchema>> GetActualColumnsAsync(
            DbConnection connection, string tableName)
        {
            var columns = new List<ColumnSchema>();

            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName})";

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var type = reader.GetString(2).ToLower();
                columns.Add(new ColumnSchema
                {
                    Name = reader.GetString(1),
                    Type = type.Contains("int") ? ColumnType.Integer :
                           type.Contains("char") ? ColumnType.Varchar : ColumnType.Text,
                    IsNullable = !reader.GetBoolean(3), // NOT NULL = false → IsNullable = true
                    IsPrimaryKey = reader.GetInt32(5) > 0
                });
            }

            return columns;
        }
    }













}
