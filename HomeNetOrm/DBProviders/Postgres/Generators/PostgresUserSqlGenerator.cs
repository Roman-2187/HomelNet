using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.Generators
{
    internal class PostgresUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public PostgresUserSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // --- БАЗОВЫЙ CRUD ОРМ (Строгое экранирование кавычками "") ---
        public override string GenerateInsert() =>
            $@"INSERT INTO ""{FormattedTable.TableName}"" ({FormattedTable.InsertFields}) VALUES ({FormattedTable.InsertParameters}) RETURNING {FormattedTable.IdColumnName ?? "id"};";

        public override string GenerateUpdate() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName}::text = @{FormattedTable.IdColumnName}::text;";

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";

        // --- СПЕЦИФИКА ПОЛЬЗОВАТЕЛЕЙ ---
        public string GenerateSelectByEmail() => $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE email = @email;";
        public string GenerateEmailExists() => $"SELECT COUNT(*) FROM \"{FormattedTable.TableName}\" WHERE email = @email;";
    }
}
