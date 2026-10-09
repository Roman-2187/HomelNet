using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using System;

namespace HomeNetOrm.DBProviders.Postgres
{
    /// <summary>
    /// ОРМ-генератор SQL-запросов таблицы друзей для центральной СУБД PostgreSQL.
    /// Учитывает экранирование двойными кавычками и специфику синтаксиса Postgres.
    /// </summary>
    public class PostgresFriendSqlGenerator : BaseSqlGenerator<FriendEntity>, ISqlGenerator<FriendEntity>
    {
        public PostgresFriendSqlGenerator(ISchemaAdapter adapter, ILogger logger) : base(adapter, logger) { }

        public override string GenerateInsert()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            // В Postgres экранируем имя таблицы и возвращаем сгенерированный ID через RETURNING
            return $@"INSERT INTO ""{FormattedTable.TableName}"" ({FormattedTable.InsertFields}) 
                      VALUES ({FormattedTable.InsertParameters}) RETURNING ""{idColumn}"";";
        }

        public override string GenerateUpdate() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET {FormattedTable.SetClause} WHERE \"{FormattedTable.IdColumnName ?? "id"}\" = @{FormattedTable.IdColumnName ?? "id"};";

        public override string GenerateDelete()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            // Строгое текстовое приведение Guid/UUID для стабильного удаления по сети
            return $"DELETE FROM \"{FormattedTable.TableName}\" WHERE \"{idColumn}\"::text = @id::text;";
        }

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE \"{FormattedTable.IdColumnName ?? "id"}\" = @id;";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";
    }
}
