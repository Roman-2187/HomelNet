using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    /// <summary>
    /// 🎯 ЧИСТЫЙ ГЕНЕРАТОР СЕТТИНГОВ: Взяли только конструктор ядра! 
    /// Нет завязки на CRUD, пишем только то, что нужно.
    /// </summary>
    internal class SqliteDeviceSettingsSqlGenerator : CoreSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        public SqliteDeviceSettingsSqlGenerator(ISchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger) { }

        public string GenerateSelectByKey()
        {
            return $@"SELECT ""id"" FROM ""{FormattedTable.TableName}"" WHERE ""device_id"" = @Key LIMIT 1;";
        }

        public string GenerateInsertOrUpdate()
        {
            // Запрос чистый, без хардкода значений. Все данные заедут через параметры Dapper!
            return $@"
        INSERT INTO ""{FormattedTable.TableName}"" 
        (""device_id"", ""id"", ""user_id"", ""email"", ""password"", ""updated_at"") 
        VALUES ( 
            @Key, 
            @Value, 
            @UserId, 
            @Email, 
            @Password, 
            CURRENT_TIMESTAMP 
        ) 
        ON CONFLICT(""device_id"", ""id"") 
        DO UPDATE SET 
            ""updated_at"" = CURRENT_TIMESTAMP;";
        }

    }
}

