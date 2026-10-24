using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using System.Collections.Generic;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteDeviceSettingsSqlGenerator : CoreSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public SqliteDeviceSettingsSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName;
            _idColumn = Schema.SinglePrimaryKey?.Name ?? "id";
        }

        public SqlQueryCommand GenerateSelectByKey() =>
            Command($@"SELECT ""{_idColumn}"" FROM ""{_tableName}"" LIMIT 1;");

        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            string fields = SqlQueryBuilder.BuildInsertFields(Schema);
            string values = SqlQueryBuilder.BuildInsertParameters(CamelSchema);

            string conflictDevice = DbColumns[nameof(DeviceSettingsEntity.DeviceId)];
            string updateSet = $"\"{DbColumns[nameof(DeviceSettingsEntity.UpdatedAt)]}\" = CURRENT_TIMESTAMP";

            string sql = $@"
                INSERT INTO ""{_tableName}"" ({fields}) 
                VALUES ({values}) 
                ON CONFLICT(""{conflictDevice}"") 
                DO UPDATE SET {updateSet};";

            return Command(sql);
        }
    }
}
