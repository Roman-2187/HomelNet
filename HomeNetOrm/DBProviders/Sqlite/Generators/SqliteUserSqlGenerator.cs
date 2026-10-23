using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public SqliteUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.CamelCase, logger) // Ядро само находит схему по типу T и кэширует полей в CamelCase
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.CamelCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name).ConvertColumnName(NameFormat.CamelCase);
        }

        // --- БАЗОВЫЙ CRUD ОРМ ---

        public override string GenerateInsert() =>
           $@"INSERT INTO ""{_tableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.CamelCase)}) 
           VALUES ({SqlQueryBuilder.BuildInsertParameters(Schema)});";

        public override string GenerateUpdate() =>
            $"UPDATE \"{_tableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, NameFormat.CamelCase)} WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public override string GenerateSelectById() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public override string GenerateSelectAll() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\";";


        // --- 🔥 СПЕЦИФИКА ПОЛЬЗОВАТЕЛЕЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ---

        public string GenerateSelectByEmail() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};";

        public string GenerateEmailExists() =>
            $"SELECT COUNT(*) FROM \"{_tableName}\" WHERE \"{DbColumns[nameof(UserEntity.Email)]}\" = {DapperParams[nameof(UserEntity.Email)]};";
    }
}
