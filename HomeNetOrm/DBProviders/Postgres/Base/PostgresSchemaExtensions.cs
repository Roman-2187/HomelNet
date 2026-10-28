using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetOrm.DBProviders.Postgres.Base
{
    internal static class PostgresSchemaExtensions
    {
        // 🧙‍♂️ Метод расширения вешается на интерфейс, но работает через транслятор Postgres!
        public static string GenerateVersionFunctionSql(this ISchemaSqlGenerator _, ISqlDialectTranslator translator)
        {
            var sb = new StringBuilder();

            // 1. Создаем глобальный счетчик в Postgres, если его еще нет
            sb.Append("CREATE SEQUENCE IF NOT EXISTS global_db_version_seq START WITH 0; ");

            // 2. Пишем функцию, которая при любой операции выдает строке новое сквозное число
            sb.Append($"{translator.Translate(Sql.Cmd.Create)} {translator.Translate(Sql.Trigger.Replace)} ")
              .Append($"{translator.Translate(Sql.Cmd.Function)} update_version_func() ")
              .Append($"{translator.Translate(Sql.Trigger.Returns)} {translator.Translate(Sql.Cmd.Trigger)} {translator.Translate(Sql.Phrase.As)} $$ ")
              .Append($"{translator.Translate(Sql.Op.Begin)} ")

              // 🧙‍♂️ Вот он — статический сквозной инкремент! Новое число при ЛЮБОМ действии
              .Append($"{translator.Translate(Sql.Trigger.New)}.version = nextval('global_db_version_seq'); ")

              .Append($"RETURN {translator.Translate(Sql.Trigger.New)}; ")
              .Append($"{translator.Translate(Sql.Op.End)} $$ ")
              .Append($"{translator.Translate(Sql.Phrase.Language)} plpgsql;");

            return sb.ToString();
        }



        public static string GenerateVersionTriggerSql(this ISchemaSqlGenerator _, TableSchema schema, ISqlDialectTranslator translator)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            string cleanTableName = schema.TableName.Replace("\"", "").ToLower();
            string triggerName = $"trg_{cleanTableName}_version";
            string formattedTable = translator.FormatTable(schema);

            var sb = new StringBuilder();
            sb.Append($"{translator.Translate(Sql.Cmd.Create)} {translator.Translate(Sql.Cmd.Trigger)} {triggerName} ")
              // 🎯 ТЕПЕРЬ СРАБАТЫВАЕТ И ПРИ ВСТАВКЕ, И ПРИ ОБНОВЛЕНИИ
              .Append($"{translator.Translate(Sql.Trigger.Before)} {translator.Translate(Sql.Cmd.Insert)} OR {translator.Translate(Sql.Cmd.Update)} ")
              .Append($"{translator.Translate(Sql.Trigger.On)} {formattedTable} ")
              .Append($"{translator.Translate(Sql.Trigger.ForEach)} {translator.Translate(Sql.Trigger.Row)} ")
              .Append($"{translator.Translate(Sql.Trigger.Execute)} update_version_func();");

            return sb.ToString();
        }


    }
}
