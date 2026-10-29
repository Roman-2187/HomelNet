using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using System.Text;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Postgres
{
    internal static class PostgresSchemaExtensions
    {
        // 🧙‍♂️ Метод расширения вешается на интерфейс, но работает через транслятор Postgres!
        public static string GenerateVersionFunctionSql(this ISchemaSqlGenerator _, ISqlDialectTranslator translator)
        {
            var sb = new StringBuilder();

            // 1. Создаем глобальный счетчик в Postgres, если его еще нет
            sb.Append($"{translator.Translate(CmdType.Create)} SEQUENCE IF NOT EXISTS {VersionSequenceName} START WITH 0; ");

            // 2. Пишем функцию, которая при любой операции выдает строке новое сквозное число
            sb.Append($"{translator.Translate(CmdType.Create)} {translator.Translate(TriggerType.Replace)} ")
              .Append($"{translator.Translate(CmdType.Function)} {VersionFunctionName}() ")
              .Append($"{translator.Translate(TriggerType.Returns)} {translator.Translate(CmdType.Trigger)} {translator.Translate(PhraseType.As)} $$ ")
              .Append($"{translator.Translate(OpType.Begin)} ")

              // 🧙‍♂️ Вот он — статический сквозной инкремент! Новое число при ЛЮБОМ действии
              .Append($"{translator.Translate(TriggerType.New)}.version = nextval('{VersionSequenceName}'); ")

              .Append($"RETURN {translator.Translate(TriggerType.New)}; ")
              .Append($"{translator.Translate(OpType.End)} $$ ")
              .Append($"{translator.Translate(PhraseType.Language)} {PostgresLanguageMarker};");

            return sb.ToString();
        }

        public static string GenerateVersionTriggerSql(this ISchemaSqlGenerator _, TableSchema schema, ISqlDialectTranslator translator)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            string cleanTableName = schema.TableName.Replace("\"", "").ToLower();
            string triggerName = $"trg_{cleanTableName}_version";
            string formattedTable = translator.FormatTable(schema);

            var sb = new StringBuilder();
            sb.Append($"{translator.Translate(CmdType.Create)} {translator.Translate(CmdType.Trigger)} {triggerName} ")
              // 🎯 ТЕПЕРЬ СРАБАТЫВАЕТ И ПРИ ВСТАВКЕ, И ПРИ ОБНОВЛЕНИИ
              .Append($"{translator.Translate(TriggerType.Before)} {translator.Translate(CmdType.Insert)} OR {translator.Translate(CmdType.Update)} ")
              .Append($"{translator.Translate(TriggerType.On)} {formattedTable} ")
              .Append($"{translator.Translate(TriggerType.ForEach)} {translator.Translate(TriggerType.Row)} ")
              .Append($"{translator.Translate(TriggerType.Execute)} {VersionFunctionName}();");

            return sb.ToString();
        }

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Специфичные константы Postgres, спрятанные от обзора
        // =========================================================================
        private const string VersionSequenceName = "global_db_version_seq";
        private const string VersionFunctionName = "update_version_func";
        private const string PostgresLanguageMarker = "plpgsql";
    }
}
