using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using System;

namespace HomeNetOrm.DBProviders.Postgres
{
    public class PostgresUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public PostgresUserSqlGenerator(ISchemaAdapter adapter, ILogger logger) : base(adapter, logger) { }

        public override string GenerateInsert()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            return $@"INSERT INTO ""{FormattedTable.TableName}"" ({FormattedTable.InsertFields}) 
                      VALUES ({FormattedTable.InsertParameters}) RETURNING {idColumn};";
        }

        public override string GenerateUpdate() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateDelete()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            return $"DELETE FROM \"{FormattedTable.TableName}\" WHERE {idColumn}::text = @{idColumn}::text;";
        }

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";

        // Специфика Пользователей
        public string GenerateSelectByEmail() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE email = @email;";

        public string GenerateEmailExists() =>
            $"SELECT COUNT(*) FROM \"{FormattedTable.TableName}\" WHERE email = @email;";
    }
}
