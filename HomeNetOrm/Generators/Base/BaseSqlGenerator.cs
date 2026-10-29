using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Base
{
    internal abstract class BaseSqlGenerator<T> : CoreSqlGenerator<T> where T : class
    {
        private readonly SqlQueryCommand _insertCommand;
        private readonly SqlQueryCommand _updateCommand;
        private readonly SqlQueryCommand _selectAllCommand;
        private readonly string _pkCamelName;

        protected BaseSqlGenerator(IEnumerable<TableSchema> availableSchemas, ISqlDialectTranslator translator, ILogger logger)
            : base(availableSchemas, translator, logger)
        {
            // Вытаскиваем имя первичного ключа из отмаппленных C# метаданных (например, "Id")
            _pkCamelName = CamelSchema.SinglePrimaryKey?.Name ?? "Id";

            // 🧵 Кэшируем неизменяемые команды один раз при старте!
            _insertCommand = BuildInsert();
            _updateCommand = BuildUpdate();
            _selectAllCommand = BuildSelectAll();
        }

        // Базовый CRUD выдает готовые команды мгновенно
        public SqlQueryCommand GenerateInsert() => _insertCommand;
        public SqlQueryCommand GenerateUpdate() => _updateCommand;
        public SqlQueryCommand GenerateSelectAll() => _selectAllCommand;

        public SqlQueryCommand GenerateDelete(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Delete).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(_pkCamelName).Emit(OpType.Equal).Param(_pkCamelName, id)
                .Build();

        public SqlQueryCommand GenerateSelectById(Guid id) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(_pkCamelName).Emit(OpType.Equal).Param(_pkCamelName, id)
                .Build();

        // --- Внутренние методы сборки шаблонов запросов ---

        private SqlQueryCommand BuildInsert()
        {
            var builder = CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters();

            var returningSql = Translator.FormatReturningId(Schema.SinglePrimaryKey?.Name ?? "id");
            return builder.Raw(returningSql).Build();
        }

        private SqlQueryCommand BuildUpdate() =>
            CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).SetFields()
                .Emit(PhraseType.Where).Col(_pkCamelName).Emit(OpType.Equal).Param(_pkCamelName, DapperParams[_pkCamelName])
                .Build();

        private SqlQueryCommand BuildSelectAll() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Build();
    }
}
