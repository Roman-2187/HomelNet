using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Sqlite
{
    public class SqliteDialectTranslator : ISqlDialectTranslator
    {
        // --- 🏗️ Блок экранирования метаданных (Таблицы и Колонки) ---

        public string FormatTable(TableSchema schema) => $"[{schema.TableName}]";
        public string FormatColumnName(string columnName) => $"[{columnName}]";

        public string FormatReturningId(string columnName) => $"; SELECT last_insert_rowid() AS [{columnName}]";

        public string FormatLimit(int limit, int? offset = null) =>
            offset.HasValue ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";

        // --- ⚡ Системные функции и кастинг типов SQLite ---

        public string FormatCastToText(string columnName) => $"CAST([{columnName}] AS TEXT)";

        public string FormatCountAll() => "COUNT(*)";
        public string FormatCountOne() => "COUNT(*)";

        // 🔥 Динамическое получение текущего времени в SQLite через нативную функцию
        public string FormatNow() => SqliteNowFunc;

        // --- 🧱 Блок трансляции перечислений из DTO Sql ---

        // 🟥 1. Команды (DML / DDL)
        public string Translate(CmdType cmd) => cmd switch
        {
            CmdType.Select => "SELECT",
            CmdType.Update => "UPDATE",
            CmdType.Delete => "DELETE",
            CmdType.Insert => "INSERT",
            CmdType.Create => "CREATE",
            CmdType.Trigger => "TRIGGER",
            CmdType.Function => "CREATE FACTORY",
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
            // 🔥 Дописываем кейсы для триггерных структур
            PhraseType.Sequence => "SEQUENCE",
            PhraseType.OrDml => "OR",
            _ => throw new ArgumentOutOfRangeException(nameof(phrase))
        };

        // 🟩 3. Операторы и логика (Истина/Ложь в SQLite — это 1 и 0)
        public string Translate(OpType op) => op switch
        {
            OpType.Equal => "=",
            OpType.And => "AND",
            OpType.Or => "OR",
            OpType.True => "1",
            OpType.False => "0",
            OpType.OpenBracket => "(",
            OpType.CloseBracket => ")",
            OpType.Asc => "ASC",
            OpType.Desc => "DESC",
            OpType.GreaterThan => ">",
            OpType.IsNull => "IS NULL",
            OpType.Begin => "BEGIN",
            OpType.End => "END;",
            // 🔥 Наша новая космическая запятая, которая убирает хардкод строк из UPSERT-ов!
            OpType.Comma => ",",
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

        // 🟦 4. Специфика триггеров под диалект SQLite
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
            TriggerType.Execute => "BEGIN",
            TriggerType.New => "NEW",
            TriggerType.Old => "OLD",
            _ => throw new ArgumentOutOfRangeException(nameof(trig))
        };

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Изолируем специфичные текстовые маркеры SQLite
        // =========================================================================
        private const string SqliteNowFunc = "datetime('now')";
    }
}
