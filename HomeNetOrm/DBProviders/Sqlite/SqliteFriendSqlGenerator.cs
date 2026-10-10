using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// ОРМ-генератор SQL-запросов таблицы друзей для локальной СУБД SQLite.
    /// </summary>
    public class SqliteFriendSqlGenerator : BaseSqlGenerator<FriendEntity>, ISqlGenerator<FriendEntity>
    {
        public SqliteFriendSqlGenerator(ISchemaAdapter adapter, ILogger logger) : base(adapter, logger) { }

        public override string GenerateInsert()
        {
            // Стандартная вставка SQLite без RETURNING
            return $@"INSERT INTO {FormattedTable.TableName} ({FormattedTable.InsertFields}) 
                      VALUES ({FormattedTable.InsertParameters});";
        }

        public override string GenerateUpdate() =>
            $"UPDATE {FormattedTable.TableName} SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName ?? "id"} = @{FormattedTable.IdColumnName ?? "id"};";

        public override string GenerateDelete() =>
            $"DELETE FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM {FormattedTable.TableName};";
    }
}
