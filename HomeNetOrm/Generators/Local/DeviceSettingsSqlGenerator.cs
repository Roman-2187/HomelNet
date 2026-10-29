using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Local
{
    // 🔥 Тоже строго от CoreSqlGenerator. Чистая спецификация под одну задачу автологина
    internal class DeviceSettingsSqlGenerator : CoreSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        public DeviceSettingsSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateSelectByKey() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            return CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict).Emit(OpType.OpenBracket).Col(d => d.DeviceId).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                .UpsertExcludedFields()
                .Build();
        }
    }
}
