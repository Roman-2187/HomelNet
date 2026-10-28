using System.Text;
using Dapper;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Builders
{
    internal class TokenQueryBuilder
    {
        private readonly ISqlDialectTranslator _tr;
        private readonly TableSchema _snakeSchema;
        private readonly TableSchema _camelSchema;
        private readonly IReadOnlyDictionary<string, string> _cols;
        private readonly StringBuilder _sb = new();
        private readonly DynamicParameters _params = new();

        public TokenQueryBuilder(ISqlDialectTranslator tr, TableSchema snakeSchema, TableSchema camelSchema, IReadOnlyDictionary<string, string> cols)
        {
            _tr = tr ?? throw new ArgumentNullException(nameof(tr));
            _snakeSchema = snakeSchema ?? throw new ArgumentNullException(nameof(snakeSchema));
            _camelSchema = camelSchema ?? throw new ArgumentNullException(nameof(camelSchema));
            _cols = cols ?? throw new ArgumentNullException(nameof(cols));
        }

        public TokenQueryBuilder Emit(CmdType cmd) { _sb.Append($"{_tr.Translate(cmd)} "); return this; }
        public TokenQueryBuilder Emit(PhraseType phrase) { _sb.Append($"{_tr.Translate(phrase)} "); return this; }
        public TokenQueryBuilder Emit(OpType op) { _sb.Append($"{_tr.Translate(op)} "); return this; }
        public TokenQueryBuilder Emit(TriggerType trig) { _sb.Append($"{_tr.Translate(trig)} "); return this; }

        public TokenQueryBuilder Table() { _sb.Append($"{_tr.FormatTable(_snakeSchema)} "); return this; }

        public TokenQueryBuilder Col(string prop)
        {
            if (_cols.TryGetValue(prop, out var dbName))
            {
                _sb.Append($"{_tr.FormatColumnName(dbName)} ");
            }
            return this;
        }

        public TokenQueryBuilder Raw(string rawSql)
        {
            _sb.Append($"{rawSql} ");
            return this;
        }

        // 🧙‍♂️ Идеальная чистота: просто просим транслятор сделать каст
        public TokenQueryBuilder ColAsText(string prop)
        {
            if (_cols.TryGetValue(prop, out var dbName))
            {
                _sb.Append($"{_tr.FormatCastToText(dbName)} ");
            }
            return this;
        }

        // 🧙‍♂️ Просто берем синтаксис агрегатов из текущего диалекта
        public TokenQueryBuilder CountAll() { _sb.Append($"{_tr.FormatCountAll()} "); return this; }
        public TokenQueryBuilder CountOne() { _sb.Append($"{_tr.FormatCountOne()} "); return this; }

        public TokenQueryBuilder Param(string paramName, object val)
        {
            _params.Add(paramName, val);
            _sb.Append($"@{paramName} ");
            return this;
        }

        public TokenQueryBuilder Fields()
        {
            var fields = _camelSchema.Columns.Zip(_snakeSchema.Columns, (c, s) =>
                $"{_tr.FormatColumnName(s.Name!)} AS {c.Name}");
            _sb.Append(string.Join(", ", fields) + " ");
            return this;
        }

        public TokenQueryBuilder InsertFields()
        {
            var insertable = _snakeSchema.Columns.Where(c => !c.IsAutoIncrement)
                .Select(c => _tr.FormatColumnName(c.Name!));
            _sb.Append($"({string.Join(", ", insertable)}) ");
            return this;
        }

        public TokenQueryBuilder InsertParameters()
        {
            var insertable = _camelSchema.Columns.Where(c => !c.IsAutoIncrement)
                .Select(c => $"@{c.Name}");
            _sb.Append($"({string.Join(", ", insertable)}) ");
            return this;
        }

        public TokenQueryBuilder SetFields()
        {
            var nonPkSnake = _snakeSchema.Columns.Where(c => !c.IsPrimaryKey).ToList();
            var nonPkCamel = _camelSchema.Columns.Where(c => !c.IsPrimaryKey).ToList();

            var pairs = nonPkSnake.Zip(nonPkCamel, (s, c) =>
                $"{_tr.FormatColumnName(s.Name!)} = @{c.Name}");

            _sb.Append(string.Join(", ", pairs) + " ");
            return this;
        }

        public SqlQueryCommand Build() => new(_sb.ToString().TrimEnd() + ";", _params);
    }
}