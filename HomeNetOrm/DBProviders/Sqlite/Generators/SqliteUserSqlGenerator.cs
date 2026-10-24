using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        protected readonly string TableName;
        protected readonly string IdColumn;

        public SqliteUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            TableName = Schema.TableName;
            IdColumn = Schema.SinglePrimaryKey?.Name ?? "id";
        }

        // --- БАЗОВЫЙ CRUD ОРМ (Заворачиваем в команды) ---

        public override SqlQueryCommand GenerateInsert() =>
            Command($@"INSERT INTO ""{TableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema)}) 
                       VALUES ({SqlQueryBuilder.BuildInsertParameters(CamelSchema)});");

        public override SqlQueryCommand GenerateUpdate() =>
            Command($"UPDATE \"{TableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, CamelSchema)} WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};");

        public override SqlQueryCommand GenerateDelete(Guid id) =>
            Command($"DELETE FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);

        public override SqlQueryCommand GenerateSelectById(Guid id) =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);

        public override SqlQueryCommand GenerateSelectAll() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\";");


        // --- 🔥 СПЕЦИФИКА ПОЛЬЗОВАТЕЛЕЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ---

        public SqlQueryCommand GenerateSelectByEmail(string email) =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};",
                    nameof(UserEntity.Email), email);

        public SqlQueryCommand GenerateEmailExists(string email) =>
            Command($"SELECT COUNT(*) FROM \"{TableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};",
                    nameof(UserEntity.Email), email);
    }
}
