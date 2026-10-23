using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Postgres.SincedGenerators
{
    internal class SyncedPostgresUserSqlGenerator : PostgresUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public SyncedPostgresUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.SnakeCase);
        }

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public string GenerateSelectUnsynced() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" " +
            $"WHERE \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = FALSE OR \"{DbColumns[nameof(UserEntity.IsSynced)]}\" IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{_tableName}\" SET \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = TRUE WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        // 🔥 ИДЕАЛЬНО: Передаем в метод репликации юзеров пустой объект UserEntity, 
        // где в поле UpdatedAt записано время чекпоинта клиента.
        public string GenerateSelectUpdatesByTimestamp() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" " +
            $"WHERE \"{DbColumns[nameof(UserEntity.UpdatedAt)]}\" > {DapperParams[nameof(UserEntity.UpdatedAt)]};";
    }
}
