using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using System;

namespace HomeNetOrm.DBProviders.Sqlite
{
    public class SqliteUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public SqliteUserSqlGenerator(ISchemaAdapter adapter, ILogger logger) : base(adapter, logger) { }

        public override string GenerateInsert() =>
            $@"INSERT INTO {FormattedTable.TableName} ({FormattedTable.InsertFields}) VALUES ({FormattedTable.InsertParameters});
            SELECT last_insert_rowid() AS id;";

        public override string GenerateUpdate() =>
            $"UPDATE {FormattedTable.TableName} SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateDelete() =>
            $"DELETE FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectById() =>
            $"SELECT * FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";

        // Специфика Пользователей
        public string GenerateSelectByEmail() =>
            $"SELECT {FormattedTable.AllFields} FROM {FormattedTable.TableName} WHERE email = @email;";

        public string GenerateEmailExists() =>
            $"SELECT COUNT(*) FROM {FormattedTable.TableName} WHERE email = @email;";
    }
}
