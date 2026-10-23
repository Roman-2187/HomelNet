using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity; // Твоя сущность для синхронизации устройств
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    /// <summary>
    /// 🔒 ЧИСТЫЙ ГЕНЕРАТОР: Серверная синхронизация девайсов на Postgres.
    /// Работает на полном статическом автопилоте без единой строки хардкода! 🎯
    /// </summary>
    internal class PostgresDeviceSyncSqlGenerator : CoreSqlGenerator<DeviceSyncEntity>, IRemoteDeviceSyncSqlGenerator
    {
        private readonly string _tableName;

        // В конструкторе принимаем схемы и логгер, регистрируя словари ядра в SnakeCase под Postgres
        public PostgresDeviceSyncSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.SnakeCase, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.SnakeCase);
        }

        // 🔥 ФИКС: Убираем string tableName из параметров! Имя таблицы и полей берем из единственной правды сущности
        public string GenerateDeviceSyncUpsert()
        {
            // Динамически собираем поля и параметры секции INSERT по схеме
            string fields = SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.SnakeCase);
            string values = SqlQueryBuilder.BuildInsertParameters(Schema);

            // Автоматически вытаскиваем имена физических полей для ON CONFLICT Postgres
            string conflictUser = DbColumns[nameof(DeviceSyncEntity.UserId)];
            string conflictDevice = DbColumns[nameof(DeviceSyncEntity.DeviceId)];

            // Блок UPDATE SET для Postgres (EXCLUDED берет данные из вставляемой строки)
            string lastSyncedCol = DbColumns[nameof(DeviceSyncEntity.UpdatedAt)];
            string updatedCol = DbColumns[nameof(DeviceSyncEntity.UpdatedAt)];

            return $@"
                INSERT INTO ""{_tableName}"" ({fields})
                VALUES ({values})
                ON CONFLICT (""{conflictUser}"", ""{conflictDevice}"") 
                DO UPDATE SET 
                    ""{lastSyncedCol}"" = EXCLUDED.""{lastSyncedCol}"", 
                    ""{updatedCol}"" = NOW();";
        }

        // 🔥 ФИКС: Убрали опасный SELECT * и хардкодные параметры
        public string GenerateSelectUpdatesByTimestamp()
        {
            return $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} 
                      FROM ""{_tableName}"" 
                      WHERE ""{DbColumns[nameof(DeviceSyncEntity.UpdatedAt)]}"" > {DapperParams[nameof(DeviceSyncEntity.UpdatedAt)]};";
        }

        // 🔥 ФИКС: Тотальный автомат без магических строк
        public string GenerateSelectUserIdByDevice()
        {
            return $@"SELECT ""{DbColumns[nameof(DeviceSyncEntity.UserId)]}"" 
                      FROM ""{_tableName}"" 
                      WHERE ""{DbColumns[nameof(DeviceSyncEntity.DeviceId)]}"" = {DapperParams[nameof(DeviceSyncEntity.DeviceId)]} 
                      LIMIT 1;";
        }
    }
}
