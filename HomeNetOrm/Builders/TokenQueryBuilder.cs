using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dapper;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Builders
{
    // 🔥 1. Добавляем <TEntity>, чтобы билдер знал структуру нашей C#-модели!
    internal class TokenQueryBuilder<TEntity> where TEntity : class
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

        // 🔥 Все методы Emit теперь возвращают TokenQueryBuilder<TEntity>
        public TokenQueryBuilder<TEntity> Emit(CmdType cmd) { _sb.Append($"{_tr.Translate(cmd)} "); return this; }
        public TokenQueryBuilder<TEntity> Emit(PhraseType phrase) { _sb.Append($"{_tr.Translate(phrase)} "); return this; }
        public TokenQueryBuilder<TEntity> Emit(OpType op) { _sb.Append($"{_tr.Translate(op)} "); return this; }
        public TokenQueryBuilder<TEntity> Emit(TriggerType trig) { _sb.Append($"{_tr.Translate(trig)} "); return this; }

        public TokenQueryBuilder<TEntity> Table() { _sb.Append($"{_tr.FormatTable(_snakeSchema)} "); return this; }

        // --- 🧙‍♂️ НОВЫЕ МЕТОДЫ НА ЛЯМБДАХ ДЛЯ АВТОКОМПЛИТА ПОЛЕЙ ---

        // Выбор колонки через лямбду: .Col(u => u.Email)
        public TokenQueryBuilder<TEntity> Col(Expression<Func<TEntity, object?>> propertyExpression)
        {
            string propName = GetPropertyName(propertyExpression);
            if (_cols.TryGetValue(propName, out var dbName))
            {
                _sb.Append($"{_tr.FormatColumnName(dbName)} ");
            }
            return this;
        }

        // Выбор колонки с приведением к тексту: .ColAsText(u => u.Id)
        public TokenQueryBuilder<TEntity> ColAsText(Expression<Func<TEntity, object?>> propertyExpression)
        {
            string propName = GetPropertyName(propertyExpression);
            if (_cols.TryGetValue(propName, out var dbName))
            {
                _sb.Append($"{_tr.FormatCastToText(dbName)} ");
            }
            return this;
        }

        // --- 🧙‍♂️ НОВЫЕ МЕТОДЫ ДЛЯ СБОРКИ ВРЕМЕННЫХ ПЕРЕМЕННЫХ (ЛИСТКОВ) ---

        // Стандартный параметр: .Param(u => u.Email, email) -> создаст @Email
        public TokenQueryBuilder<TEntity> Param(Expression<Func<TEntity, object?>> propertyExpression, object val)
        {
            string paramName = GetPropertyName(propertyExpression);
            _params.Add(paramName, val);
            _sb.Append($"@{paramName} ");
            return this;
        }

        // 🔥 ТОТ САМЫЙ МЕТОД: Автоматический суффикс для Value1 / Value2 без хардкода строк!
        // Вызов: .Param(m => m.SenderId, "1", senderId) -> создаст @SenderId1
        public TokenQueryBuilder<TEntity> Param(Expression<Func<TEntity, object?>> propertyExpression, string suffix, object val)
        {
            string baseParamName = GetPropertyName(propertyExpression);
            string uniqueParamName = baseParamName + suffix;

            _params.Add(uniqueParamName, val);
            _sb.Append($"@{uniqueParamName} ");
            return this;
        }

        // --- Строковые методы-фолбэки (оставляем для базового CRUD в файле BaseSqlGenerator) ---
        public TokenQueryBuilder<TEntity> Col(string prop)
        {
            if (_cols.TryGetValue(prop, out var dbName))
            {
                _sb.Append($"{_tr.FormatColumnName(dbName)} ");
            }
            return this;
        }

        public TokenQueryBuilder<TEntity> Param(string paramName, object val)
        {
            _params.Add(paramName.TrimStart('@'), val);
            _sb.Append($"@{paramName} ");
            return this;
        }

        // --- Остальные методы генерации кусков SQL ---
        public TokenQueryBuilder<TEntity> Raw(string rawSql) { _sb.Append($"{rawSql} "); return this; }
        public TokenQueryBuilder<TEntity> CountAll() { _sb.Append($"{_tr.FormatCountAll()} "); return this; }
        public TokenQueryBuilder<TEntity> CountOne() { _sb.Append($"{_tr.FormatCountOne()} "); return this; }

        public TokenQueryBuilder<TEntity> Fields()
        {
            var fields = _camelSchema.Columns.Zip(_snakeSchema.Columns, (c, s) =>
                $"{_tr.FormatColumnName(s.Name!)} AS {c.Name}");
            _sb.Append(string.Join(", ", fields) + " ");
            return this;
        }

        public TokenQueryBuilder<TEntity> InsertFields()
        {
            var insertable = _snakeSchema.Columns.Where(c => !c.IsAutoIncrement)
                .Select(c => _tr.FormatColumnName(c.Name!));
            _sb.Append($"({string.Join(", ", insertable)}) ");
            return this;
        }

        public TokenQueryBuilder<TEntity> InsertParameters()
        {
            var insertable = _camelSchema.Columns.Where(c => !c.IsAutoIncrement)
                .Select(c => $"@{c.Name}");
            _sb.Append($"({string.Join(", ", insertable)}) ");
            return this;
        }

        public TokenQueryBuilder<TEntity> SetFields()
        {
            var nonPkSnake = _snakeSchema.Columns.Where(c => !c.IsPrimaryKey).ToList();
            var nonPkCamel = _camelSchema.Columns.Where(c => !c.IsPrimaryKey).ToList();

            var pairs = nonPkSnake.Zip(nonPkCamel, (s, c) =>
                $"{_tr.FormatColumnName(s.Name!)} = @{c.Name}");

            _sb.Append(string.Join(", ", pairs) + " ");
            return this;
        }

        // 🔥 Добавляем токен запятой прямо в тело запроса
        // В PostgresDialectTranslator и SqliteDialectTranslator в switch (op) допиши:
        // OpType.Comma => ",",

        // 🧙‍♂️ Метод автоматической сборки блока EXCLUDED для UPSERT
        public TokenQueryBuilder<TEntity> UpsertExcludedFields()
        {
            // Берем все колонки из snake-схемы, которые НЕ являются первичным ключом
            var nonPkSnake = _snakeSchema.Columns.Where(c => !c.IsPrimaryKey).ToList();

            // Автоматически штампуем пары вида: "user_id" = EXCLUDED."user_id"
            // Используем правильное экранирование кавычек конкретного диалекта текущей СУБД!
            var pairs = nonPkSnake.Select(c =>
                $"{_tr.FormatColumnName(c.Name!)} = EXCLUDED.{_tr.FormatColumnName(c.Name!)}");

            // Склеиваем через запятую и пушим в StringBuilder запроса
            _sb.Append(string.Join(", ", pairs) + " ");
            return this;
        }

        public SqlQueryCommand Build() => new(_sb.ToString().TrimEnd() + ";", _params);

        // --- ⚙️ ВНУТРЕННИЙ ПАРСЕР ДЕРЕВЬЕВ ВЫРАЖЕНИЙ .NET ---
        private string GetPropertyName(Expression<Func<TEntity, object?>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            Expression body = expression.Body;

            // Раздеваем UnaryExpression (Convert) для Guid, DateTimeOffset, int и т.д.
            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            if (body is MemberExpression memberExpression &&
                (memberExpression.Member is PropertyInfo || memberExpression.Member is FieldInfo))
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException($"[SiberNet ORM] Невалидная лямбда '{expression}'. Ожидается свойство класса {typeof(TEntity).Name}.");
        }
    }
}
