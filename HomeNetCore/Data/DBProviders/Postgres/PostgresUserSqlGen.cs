using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.Schemes;

namespace HomeNetCore.Data.DBProviders.Sqlite
{
    public class PostgresUserSqlGen //: IUserSqlGenerator
    {
        private readonly TableSchema tableSchema;
        private readonly string fields;
        private readonly string parameters;
        private readonly string setClause;

        public PostgresUserSqlGen(TableSchema tableSchema)
        {
            this.tableSchema = tableSchema;

            // Формируем основные строки с учетом особенностей PostgreSQL
            fields = string.Join(", ", tableSchema.Columns.Select(c => c.Name));
            parameters = string.Join(", ", tableSchema.Columns.Select(c => $"@{c.Name}"));
            setClause = string.Join(", ", tableSchema.Columns.Select(c => $"{c.Name} = @{c.Name}"));
        }

        public string GenerateInsert()
        {
            // В PostgreSQL можно использовать более короткий синтаксис
            return $"INSERT INTO {tableSchema.TableName} ({fields}) VALUES ({parameters})";
        }

        public string GenerateUpdate()
        {
            return $"UPDATE {tableSchema.TableName} SET {setClause} WHERE Id = @Id";
        }

        public string GenerateDelete()
        {
            return $"DELETE FROM {tableSchema.TableName} WHERE Id = @Id";
        }

        public string GenerateSelectById()
        {
            return $"SELECT {fields} FROM {tableSchema.TableName} WHERE Id = @Id";
        }

        public string GenerateSelectByEmail()
        {
            return $"SELECT {fields} FROM {tableSchema.TableName} WHERE Email = @Email";
        }

        public string GenerateSelectAll()
        {
            return $"SELECT {fields} FROM {tableSchema.TableName}";
        }

        // Дополнительные методы для PostgreSQL
        public string GenerateUpsert()
        {
            return $@"
          INSERT INTO {tableSchema.TableName} ({fields}) 
          VALUES ({parameters})
          ON CONFLICT (Id) 
          DO UPDATE SET {setClause}";
        }

        public string GenerateSelectCount()
        {
            return $"SELECT COUNT(*) FROM {tableSchema.TableName}";
        }
    }


}
