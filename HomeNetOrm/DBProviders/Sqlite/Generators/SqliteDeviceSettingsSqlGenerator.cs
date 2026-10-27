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
            _idColumn = Schema.SinglePrimaryKey?.Name ?? "device_id";
        }

        // 🔥 ПОЧИНКА 1: Выбираем ВСЕ поля (*), чтобы Dapper мог полностью собрать сущность, а не только ID
        public SqlQueryCommand GenerateSelectByKey() =>
            Command($@"SELECT * FROM ""{_tableName}"" LIMIT 1;");

        // 🔥 ПОЧИНКА 2: Добавляем обновление ВСЕХ полей сессии при конфликте
        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            string fields = SqlQueryBuilder.BuildInsertFields(Schema);
            string values = SqlQueryBuilder.BuildInsertParameters(CamelSchema);

            string conflictDevice = DbColumns[nameof(DeviceSettingsEntity.DeviceId)];

            // Вытаскиваем snake_case названия колонок из маппинга DbColumns
            string userCol = DbColumns[nameof(DeviceSettingsEntity.UserId)];
            string emailCol = DbColumns[nameof(DeviceSettingsEntity.Email)];
            string passCol = DbColumns[nameof(DeviceSettingsEntity.Password)];
            string updatedCol = DbColumns[nameof(DeviceSettingsEntity.UpdatedAt)];

            // Формируем полноценный UPDATE SET для SQLite через EXCLUDED
            string updateSet = $@"
                ""{userCol}"" = EXCLUDED.""{userCol}"",
                ""{emailCol}"" = EXCLUDED.""{emailCol}"",
                ""{passCol}"" = EXCLUDED.""{passCol}"",
                ""{updatedCol}"" = EXCLUDED.""{updatedCol}""";

            string sql = $@"
                INSERT INTO ""{_tableName}"" ({fields}) 
                VALUES ({values}) 
                ON CONFLICT(""{conflictDevice}"") 
                DO UPDATE SET {updateSet};";

            return Command(sql);
        }
    }
}
