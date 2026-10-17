using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite
{
    /// <summary>
    /// ОРМ-генератор SQL-запросов таблицы друзей для локальной СУБД SQLite.
    /// </summary>
    internal class SqliteFriendSqlGenerator : BaseSqlGenerator<FriendEntity>, ISqlGenerator<FriendEntity>
    {
        // Добавляем IEnumerable<TableSchema> schemas в параметры и пробрасываем в base()
        public SqliteFriendSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }


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
