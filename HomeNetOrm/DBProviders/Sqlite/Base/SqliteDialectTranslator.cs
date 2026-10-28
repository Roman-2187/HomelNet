using System;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Sqlite
{
    public class SqliteDialectTranslator : ISqlDialectTranslator
    {
        // 🧙‍♂️ SQLite любит квадратные скобки для экранирования таблиц и колонок
        public string FormatTable(TableSchema schema) => $"[{schema.TableName}]";
        public string FormatColumnName(string columnName) => $"[{columnName}]";

        public string FormatLimit(int limit, int? offset = null) =>
            offset.HasValue ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";

        // 🧙‍♂️ В SQLite кастинг к тексту делается через классический CAST(... AS TEXT)
        public string FormatCastToText(string columnName) => $"CAST([{columnName}] AS TEXT)";

        // 🧙‍♂️ В SQLite COUNT(1) и COUNT(*) работают одинаково эффективно
        public string FormatCountAll() => "COUNT(*)";
        public string FormatCountOne() => "COUNT(*)";

        // 🟥 Команды СУБД
        public string Translate(CmdType cmd) => cmd switch
        {
            CmdType.Select => "SELECT",
            CmdType.Update => "UPDATE",
            CmdType.Delete => "DELETE",
            CmdType.Insert => "INSERT",
            CmdType.Create => "CREATE",
            CmdType.Trigger => "TRIGGER",
            CmdType.Function => "CREATE FACTORY", // Или любая заглушка, если триггеры SQLite собираются иначе
            CmdType.Procedure => "PROCEDURE",
            _ => throw new ArgumentOutOfRangeException(nameof(cmd))
        };

        // 🟨 Ключевые слова-связки
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
            _ => throw new ArgumentOutOfRangeException(nameof(phrase))
        };

        // 🟩 Операторы и логика (Тут SQLite вместо TRUE/FALSE использует 1 и 0)
        public string Translate(OpType op) => op switch
        {
            OpType.Equal => "=",
            OpType.And => "AND",
            OpType.Or => "OR",
            OpType.True => "1",  // 🔥 В SQLite истина — это единица
            OpType.False => "0",  // 🔥 В SQLite ложь — это ноль
            OpType.OpenBracket => "(",
            OpType.CloseBracket => ")",
            OpType.Asc => "ASC",
            OpType.Desc => "DESC",
            OpType.GreaterThan => ">",
            OpType.IsNull => "IS NULL",
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

        // 🟦 Специфика триггеров под диалект SQLite
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
            TriggerType.Execute => "BEGIN", // В SQLite тело триггера начинается с BEGIN вместо EXECUTE FUNCTION
            TriggerType.New => "NEW",
            TriggerType.Old => "OLD",
            _ => throw new ArgumentOutOfRangeException(nameof(trig))
        };
    }
}