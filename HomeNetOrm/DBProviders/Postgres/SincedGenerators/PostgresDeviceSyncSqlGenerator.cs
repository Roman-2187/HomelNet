using System;
using System;
using System.Collections.Generic;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SincedGenerators
{
    internal class PostgresDeviceSyncSqlGenerator : CoreSqlGenerator<DeviceSyncEntity>, IRemoteDeviceSyncSqlGenerator
    {
        public PostgresDeviceSyncSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateDeviceSyncUpsert()
        {
            // 🧙‍♂️ Сборка сложного UPSERT запроса без единой сырой строки и без ручных склеек .Build().Sql
            return CreateQuery()
                .Emit(Sql.Cmd.Insert).Emit(Sql.Phrase.Into).Table()
                .InsertFields()
                .Emit(Sql.Phrase.Values)
                .InsertParameters()

                // ON CONFLICT ("user_id", "device_id")
                .Emit(Sql.Phrase.On).Emit(Sql.Phrase.Conflict)
                .Emit(Sql.Op.OpenBracket)
                    .Col(nameof(DeviceSyncEntity.UserId))
                    .Raw(",") // Для запятой между колонками можно использовать точечный Raw или добавить OpType.Comma
                    .Col(nameof(DeviceSyncEntity.DeviceId))
                .Emit(Sql.Op.CloseBracket)

                // DO UPDATE SET "updated_at" = NOW()
                .Emit(Sql.Phrase.Do).Emit(Sql.Cmd.Update).Emit(Sql.Phrase.Set)
                .Col(nameof(DeviceSyncEntity.UpdatedAt)).Emit(Sql.Op.Equal).Raw("NOW()")
                .Build();
        }

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(DeviceSyncEntity.UpdatedAt)).Emit(Sql.Op.GreaterThan)
                    .Param(nameof(DeviceSyncEntity.UpdatedAt), timestamp)
                .Build();

        public SqlQueryCommand GenerateSelectUserIdByDevice(Guid deviceId) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Col(nameof(DeviceSyncEntity.UserId)).Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(DeviceSyncEntity.DeviceId)).Emit(Sql.Op.Equal)
                    .Param(nameof(DeviceSyncEntity.DeviceId), deviceId)
                .Raw(Translator.FormatLimit(1)) // Извлекаем чистый лимит из диалекта СУБД вместо строки "LIMIT 1"
                .Build();
    }
}