using System;
using System.Collections.Generic;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Enums;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Remote
{
    // 🔥 Наследуемся строго от CoreSqlGenerator! Никакого лишнего CRUD-мусора
    internal class DeviceSyncSqlGenerator :BaseSqlGenerator<DeviceSyncEntity>, IRemoteDeviceSyncSqlGenerator
    {
        public DeviceSyncSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateDeviceSyncUpsert()
        {
            return CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict)
                .Emit(OpType.OpenBracket)
                    .Col(d => d.UserId)
                    .Emit(OpType.Comma) 
                    .Col(d => d.DeviceId)
                .Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                .Col(d => d.UpdatedAt).Emit(OpType.Equal).Raw(Translator.FormatNow()) 
                .Build();
        }

        public SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(d => d.UpdatedAt).Emit(OpType.GreaterThan)
                    .Param(d => d.UpdatedAt, timestamp)
                .Build();

        public SqlQueryCommand GenerateSelectUserIdByDevice(Guid deviceId) =>
            CreateQuery()
                .Emit(CmdType.Select).Col(d => d.UserId).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(d => d.DeviceId).Emit(OpType.Equal)
                    .Param(d => d.DeviceId, deviceId)
                .Raw(Translator.FormatLimit(1)) 
                .Build();
    }
}
