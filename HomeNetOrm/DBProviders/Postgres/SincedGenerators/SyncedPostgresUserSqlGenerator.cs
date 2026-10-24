using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SincedGenerators
{
    internal class SyncedPostgresUserSqlGenerator : PostgresUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedPostgresUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            Command($"SELECT COUNT(1) FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);

        public SqlQueryCommand GenerateSelectUnsynced() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" " +
                    $"WHERE \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = FALSE OR \"{DbColumns[nameof(UserEntity.IsSynced)]}\" IS NULL;");

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            Command($"UPDATE \"{TableName}\" SET \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = TRUE WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" " +
                    $"WHERE \"{DbColumns[nameof(UserEntity.UpdatedAt)]}\" > {DapperParams[nameof(UserEntity.UpdatedAt)]};",
                    nameof(UserEntity.UpdatedAt), timestamp);
    }
}
