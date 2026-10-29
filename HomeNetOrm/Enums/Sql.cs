namespace HomeNetOrm.Enums
{
    public static class Sql
    {
        // 🟥 1. Команды
        public enum CmdType { Select, Update, Delete, Insert, Create, Trigger, Function, Procedure }
        public static class Cmd
        {
            public const CmdType Select = CmdType.Select;
            public const CmdType Update = CmdType.Update;
            public const CmdType Delete = CmdType.Delete;
            public const CmdType Insert = CmdType.Insert;
            public const CmdType Create = CmdType.Create;
            public const CmdType Trigger = CmdType.Trigger;
            public const CmdType Function = CmdType.Function;
            public const CmdType Procedure = CmdType.Procedure;
        }

        // 🟨 2. Ключевые слова-связки
       
        public enum PhraseType { From, Where, Set, Into, Values, Returning, OrderBy, Language, On, Conflict, Do,
            As,
            Sequence,
            OrDml
        }

        public static class Phrase
        {
            public const PhraseType From = PhraseType.From;
            public const PhraseType Where = PhraseType.Where;
            public const PhraseType Set = PhraseType.Set;
            public const PhraseType Into = PhraseType.Into;
            public const PhraseType Values = PhraseType.Values;
            public const PhraseType Returning = PhraseType.Returning;
            public const PhraseType OrderBy = PhraseType.OrderBy;
            public const PhraseType Language = PhraseType.Language;

            // 🆕 Добавляем 3 недостающие константы для IntelliSense:
            public const PhraseType On = PhraseType.On;
            public const PhraseType Conflict = PhraseType.Conflict;
            public const PhraseType Do = PhraseType.Do;

            public const PhraseType As = PhraseType.As;
        }

        // 🟩 3. Операторы и логика

        public enum OpType { Equal, And, Or, True, False, OpenBracket, CloseBracket, Asc, Desc, GreaterThan, IsNull,
            Begin,
            End,
            Comma
          
        }

        public static class Op
        {
            public const OpType Equal = OpType.Equal;
            public const OpType And = OpType.And;
            public const OpType Or = OpType.Or;
            public const OpType True = OpType.True;
            public const OpType False = OpType.False;
            public const OpType OpenBracket = OpType.OpenBracket;
            public const OpType CloseBracket = OpType.CloseBracket;
            public const OpType Asc = OpType.Asc;
            public const OpType Desc = OpType.Desc;
            public const OpType GreaterThan = OpType.GreaterThan;

            // 🆕 Добавляем последнюю недостающую константу для проверки на NULL:
            public const OpType IsNull = OpType.IsNull;

            public const OpType Begin = OpType.Begin;
            public const OpType End = OpType.End;
        }

        // 🟦 4. Специфика триггеров
        public enum TriggerType { Returns, Replace, Before, After, InsteadOf, On, ForEach, Row, Execute, New, Old }
        public static class Trigger
        {
            public const TriggerType Returns = TriggerType.Returns;
            public const TriggerType Replace = TriggerType.Replace;
            public const TriggerType Before = TriggerType.Before;
            public const TriggerType After = TriggerType.After;
            public const TriggerType InsteadOf = TriggerType.InsteadOf;
            public const TriggerType On = TriggerType.On;
            public const TriggerType ForEach = TriggerType.ForEach;
            public const TriggerType Row = TriggerType.Row;
            public const TriggerType Execute = TriggerType.Execute;
            public const TriggerType New = TriggerType.New;
            public const TriggerType Old = TriggerType.Old;
        }
    }
}
