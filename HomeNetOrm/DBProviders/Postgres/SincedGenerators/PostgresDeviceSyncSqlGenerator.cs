using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    internal class PostgresDeviceSyncSqlGenerator : CoreSqlGenerator<DeviceSyncEntity>, IRemoteDeviceSyncSqlGenerator
    {
        private readonly string _tableName;

        public PostgresDeviceSyncSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName;
        }

        public SqlQueryCommand GenerateDeviceSyncUpsert()
        {
            string fields = SqlQueryBuilder.BuildInsertFields(Schema);
            string values = SqlQueryBuilder.BuildInsertParameters(CamelSchema);

            string conflictUser = DbColumns[nameof(DeviceSyncEntity.UserId)];
            string conflictDevice = DbColumns[nameof(DeviceSyncEntity.DeviceId)];

            string lastSyncedCol = DbColumns[nameof(DeviceSyncEntity.UpdatedAt)];
            string updatedCol = DbColumns[nameof(DeviceSyncEntity.UpdatedAt)];

            string sql = $@"
                INSERT INTO ""{_tableName}"" ({fields})
                VALUES ({values})
                ON CONFLICT (""{conflictUser}"", ""{conflictDevice}"") 
                DO UPDATE SET 
                    ""{lastSyncedCol}"" = EXCLUDED.""{lastSyncedCol}"", 
                    ""{updatedCol}"" = NOW();";

            return Command(sql);
        }

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            Command($@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} 
                      FROM ""{_tableName}"" 
                      WHERE ""{DbColumns[nameof(DeviceSyncEntity.UpdatedAt)]}"" > {DapperParams[nameof(DeviceSyncEntity.UpdatedAt)]};",
                    nameof(DeviceSyncEntity.UpdatedAt), timestamp);

        public SqlQueryCommand GenerateSelectUserIdByDevice(Guid deviceId) =>
            Command($@"SELECT ""{DbColumns[nameof(DeviceSyncEntity.UserId)]}"" 
                      FROM ""{_tableName}"" 
                      WHERE ""{DbColumns[nameof(DeviceSyncEntity.DeviceId)]}"" = {DapperParams[nameof(DeviceSyncEntity.DeviceId)]} 
                      LIMIT 1;",
                    nameof(DeviceSyncEntity.DeviceId), deviceId);
    }
}
