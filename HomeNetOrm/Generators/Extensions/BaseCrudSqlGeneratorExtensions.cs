using HomeNet.Models.Shared.Entity; // Где лежит твоя BaseEntity
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Весь базовый CRUD теперь здесь! Чистый Fluent без хардкода полей.
   internal static class BaseCrudSqlGeneratorExtensions
    {
      

        public static SqlQueryCommand GenerateInsert<T>(this ISqlGenerator<T> generator) where T : BaseEntity
        {
            // 🔥 Кастуем к конкретному базовому классу вместо dynamic!
            var baseGen = (BaseSqlGenerator<T>)generator;

            var builder = generator.CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters();

            // Теперь Translator пробивается со строгой типизацией
            string returningSql = baseGen.Translator.FormatReturningId("id");

            return builder.Raw(returningSql).Build();
        }

        public static SqlQueryCommand GenerateUpdate<T>(this ISqlGenerator<T> generator) where T : BaseEntity
        {
            // 🔥 Кастуем к конкретному базовому классу вместо dynamic!
            var baseGen = (BaseSqlGenerator<T>)generator;

            return generator.CreateQuery()
                .Emit(CmdType.Update).Table()
                .Emit(PhraseType.Set).SetFields()
                // 🔥 Теперь лямбда скомпилируется идеально, так как вызов строго типизирован!
                .Emit(PhraseType.Where).Col(e => e.Id).Emit(OpType.Equal).Param(e => e.Id, baseGen.DapperParams["Id"])
                .Build();
        }


        public static SqlQueryCommand GenerateSelectAll<T>(this ISqlGenerator<T> generator) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Build();
        }

        public static SqlQueryCommand GenerateDelete<T>(this ISqlGenerator<T> generator, Guid id) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Delete).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(e => e.Id).Emit(OpType.Equal).Param(e => e.Id, id)
                .Build();
        }

        public static SqlQueryCommand GenerateSelectById<T>(this ISqlGenerator<T> generator, Guid id) where T : BaseEntity
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(e => e.Id).Emit(OpType.Equal).Param(e => e.Id, id)
                .Build();
        }
    }
}
