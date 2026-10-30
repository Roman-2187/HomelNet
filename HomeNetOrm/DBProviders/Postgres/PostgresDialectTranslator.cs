using System;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Postgres
{
    public class PostgresDialectTranslator : ISqlDialectTranslator
    {
        // --- 🏗️ Блок экранирования метаданных (Таблицы и Колонки) ---

        public string FormatTable(TableSchema schema) => $"\"{GenericSchemaAdapter.ToSnakeCase(schema.TableName)}\"";

        public string FormatColumnName(string columnName) => $"\"{columnName}\"";

        public string FormatReturningId(string columnName) => $"RETURNING \"{columnName}\"";

        public string FormatLimit(int limit, int? offset = null) =>
            offset.HasValue ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";

        // --- ⚡ Системные функции и кастинг типов Postgres ---

        public string FormatCastToText(string expression) => $"{expression}::text";

        public string FormatCountAll() => "COUNT(*)";

        public string FormatCountOne() => "COUNT(1)";

        // 🔥 Наш новый метод динамического получения текущего времени СУБД
        public string FormatNow() => PostgresNowFunc;

        // --- 🧱 Блок трансляции compact перечислений из DTO Sql ---

        // 🟥 1. Команды (DML / DDL)
        public string Translate(CmdType cmd) => cmd switch
        {
            CmdType.Select => "SELECT",
            CmdType.Update => "UPDATE",
            CmdType.Delete => "DELETE",
            CmdType.Insert => "INSERT",
            CmdType.Create => "CREATE",
            CmdType.Trigger => "TRIGGER",
            CmdType.Function => "FUNCTION",
            CmdType.Procedure => "PROCEDURE",
            _ => throw new ArgumentOutOfRangeException(nameof(cmd))
        };

        // 🟨 2. Ключевые слова-связки
        public string Translate(PhraseType phrase) => phrase switch
        {
            PhraseType.From => "FROM",
            PhraseType.Where => "WHERE",
            PhraseType.Set => "SET",
            PhraseType.Into => "INTO",
            PhraseType.Values => "VALUES",
            PhraseType.Returning => "RETURNING",
            PhraseType.OrderBy => "ORDER BY",
            PhraseType.Language => "LANGUAGE",
            PhraseType.On => "ON",
            PhraseType.Conflict => "CONFLICT",
            PhraseType.Do => "DO",
            PhraseType.As => "AS",
            // 🔥 Дописываем новые кейсы для триггерных расширений схем:
            PhraseType.Sequence => "SEQUENCE",
            PhraseType.OrDml => "OR",
            _ => throw new ArgumentOutOfRangeException(nameof(phrase))
        };

        // 🟩 3. Операторы, логика и границы блоков
        public string Translate(OpType op) => op switch
        {
            OpType.Equal => "=",
            OpType.And => "AND",
            OpType.Or => "OR",
            OpType.True => "TRUE",
            OpType.False => "FALSE",
            OpType.OpenBracket => "(",
            OpType.CloseBracket => ")",
            OpType.Asc => "ASC",
            OpType.Desc => "DESC",
            OpType.GreaterThan => ">",
            OpType.IsNull => "IS NULL",
            OpType.Begin => "BEGIN",
            OpType.End => "END;",
            OpType.Comma => ",",
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

        // 🟦 4. Специфика триггеров
        public string Translate(TriggerType trig) => trig switch
        {
            TriggerType.Returns => "RETURNS",
            TriggerType.Replace => "OR REPLACE",
            TriggerType.Before => "BEFORE",
            TriggerType.After => "AFTER",
            TriggerType.InsteadOf => "INSTEAD OF",
            TriggerType.On => "ON",
            TriggerType.ForEach => "FOR EACH",
            TriggerType.Row => "ROW",
            TriggerType.Execute => "EXECUTE FUNCTION",
            TriggerType.New => "NEW",
            TriggerType.Old => "OLD",
            _ => throw new ArgumentOutOfRangeException(nameof(trig))
        };

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Изолируем специфичные текстовые маркеры Postgres
        // =========================================================================
        private const string PostgresNowFunc = "NOW()";
        private const string ProceduralLanguageMarker = "plpgsql";
    }
}
