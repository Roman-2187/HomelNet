using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SyncedGenerators
{
    internal class SyncedSqliteUserSqlGenerator : SqliteUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedSqliteUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger) { }

        // === 🎯 ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ С УЧЕТОМ GUID PARAMETERS ===

        // 🔥 ФИКС: Добавляем Guid id и прокидываем в фабрику Command
        public SqlQueryCommand GenerateCountById(Guid id) =>
            Command($"SELECT COUNT(1) FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);

        public SqlQueryCommand GenerateSelectUnsynced() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" " +
                    $"WHERE \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = 0 OR \"{DbColumns[nameof(UserEntity.IsSynced)]}\" IS NULL;");

        // 🔥 ФИКС: Добавляем Guid id и сюда тоже
        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            Command($"UPDATE \"{TableName}\" SET \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = 1 WHERE \"{IdColumn}\" = {DapperParams[nameof(UserEntity.Id)]};",
                    nameof(UserEntity.Id), id);
    }
}
