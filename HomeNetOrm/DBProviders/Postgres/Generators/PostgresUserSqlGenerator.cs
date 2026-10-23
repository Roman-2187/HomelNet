using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Postgres.Generators
{
    internal class PostgresUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public PostgresUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.SnakeCase, logger) // Ядро само находит схему по типу T и кэширует поля в snake_case
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.SnakeCase);
        }

        // --- БАЗОВЫЙ CRUD ОРМ ---

        public override string GenerateInsert() =>
            $@"INSERT INTO ""{_tableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.SnakeCase)}) 
               VALUES ({SqlQueryBuilder.BuildInsertParameters(Schema)}) RETURNING ""{_idColumn}"";";

        public override string GenerateUpdate() =>
            $"UPDATE \"{_tableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, NameFormat.SnakeCase)} WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\"::text = {DapperParams[nameof(UserEntity.Id)]}::text;";

        public override string GenerateSelectById() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public override string GenerateSelectAll() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\";";


        // --- 🔥 СПЕЦИФИКА ПОЛЬЗОВАТЕЛЕЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ---

        public string GenerateSelectByEmail() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};";

        public string GenerateEmailExists() =>
            $"SELECT COUNT(*) FROM \"{_tableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};";
    }
}
