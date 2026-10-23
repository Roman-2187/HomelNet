using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;
using System.Collections.Generic;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    /// <summary>
    /// 🎯 ЧИСТЫЙ ГЕНЕРАТОР СЕТТИНГОВ: Наследуется строго от ядра CoreSqlGenerator!
    /// Пишем только то, что нужно, без бестолковых заглушек базового CRUD.
    /// </summary>
    internal class SqliteDeviceSettingsSqlGenerator : CoreSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        // В конструкторе принимаем схемы и логгер, пробрасывая их в ядро вместе с форматом SQLite
        public SqliteDeviceSettingsSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.CamelCase, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.CamelCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.CamelCase);
        }

        // --- РЕАЛИЗАЦИЯ СПЕЦИФИКИ СЕТТИНГОВ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ---

        public string GenerateSelectByKey()
        {
            // Полный автомат по nameof публичных свойств
            return $@"SELECT ""{_idColumn}"" FROM ""{_tableName}"" WHERE ""{DbColumns[nameof(DeviceSettingsEntity.DeviceId)]}"" = {DapperParams[nameof(DeviceSettingsEntity.DeviceId)]} LIMIT 1;";
        }

        public string GenerateInsertOrUpdate()
        {
            // Нарезаем поля и параметры для секции INSERT динамически по схеме
            string fields = SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.CamelCase);
            string values = SqlQueryBuilder.BuildInsertParameters(Schema);

           
            string conflictDevice = DbColumns[nameof(DeviceSettingsEntity.DeviceId)];

            // Блок UPDATE SET для таймстампа обновления
            string updateSet = $"{DbColumns[nameof(DeviceSettingsEntity.UpdatedAt)]} = CURRENT_TIMESTAMP";

            return $@"
        INSERT INTO ""{_tableName}"" ({fields}) 
        VALUES ({values}) 
        ON CONFLICT(""{conflictDevice}"") 
        DO UPDATE SET {updateSet};";
        }
    }
}
