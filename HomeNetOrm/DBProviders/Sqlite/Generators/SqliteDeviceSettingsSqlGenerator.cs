using System;
using System.Collections.Generic;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteDeviceSettingsSqlGenerator : CoreSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        public SqliteDeviceSettingsSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateSelectByKey() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Raw("*").Emit(Sql.Phrase.From).Table()
                .Raw("LIMIT 1")
                .Build();

        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            var userCol = DbColumns[nameof(DeviceSettingsEntity.UserId)];
            var emailCol = DbColumns[nameof(DeviceSettingsEntity.Email)];
            var passCol = DbColumns[nameof(DeviceSettingsEntity.Password)];
            var updatedCol = DbColumns[nameof(DeviceSettingsEntity.UpdatedAt)];

            // Специфичный блок EXCLUDED собираем через изолированный .Raw() без кавычек в теле метода
            string updateSet = $"{Translator.FormatColumnName(userCol)} = EXCLUDED.{Translator.FormatColumnName(userCol)}, " +
                               $"{Translator.FormatColumnName(emailCol)} = EXCLUDED.{Translator.FormatColumnName(emailCol)}, " +
                               $"{Translator.FormatColumnName(passCol)} = EXCLUDED.{Translator.FormatColumnName(passCol)}, " +
                               $"{Translator.FormatColumnName(updatedCol)} = EXCLUDED.{Translator.FormatColumnName(updatedCol)}";

            return CreateQuery()
                .Emit(Sql.Cmd.Insert).Emit(Sql.Phrase.Into).Table()
                .InsertFields()
                .Emit(Sql.Phrase.Values)
                .InsertParameters()
                .Emit(Sql.Phrase.On).Emit(Sql.Phrase.Conflict).Emit(Sql.Op.OpenBracket).Col(nameof(DeviceSettingsEntity.DeviceId)).Emit(Sql.Op.CloseBracket)
                .Emit(Sql.Phrase.Do).Emit(Sql.Cmd.Update).Emit(Sql.Phrase.Set).Raw(updateSet)
                .Build();
        }
    }
}
