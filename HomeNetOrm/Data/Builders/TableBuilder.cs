using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HomeNetOrm.Data.Schemes.CreateSchemaBd;

namespace HomeNetOrm.Data.Builders
{
    // 1. Добавляем <TEntity>, чтобы билдер знал, для какого класса (модели) строится таблица
    public class TableBuilder<TEntity> where TEntity : class
    {
        private readonly string _tableName;

        // 2. Исправляем List<Tab> на список билдеров колонок для нашей сущности
        private readonly List<ColumnBuilder<TEntity>> _columnBuilders = new();

        public TableBuilder(string tableName) => _tableName = tableName;

        // 3. Вместо строки name принимаем C#-выражение (например: x => x.Id)
        public ColumnBuilder<TEntity> AddColumn(Expression<Func<TEntity, object?>> propertyExpression)
        {
            var builder = new ColumnBuilder<TEntity>(propertyExpression);
            _columnBuilders.Add(builder);
            return builder;
        }

        // 4. Финализируем сборку схемы таблицы
        public TableSchema Generate()
        {
            var columns = _columnBuilders
                .Select(builder => builder.Build()) // Сначала собираем все колонки
                .ToList();

            // Считаем, сколько колонок помечено как Primary Key
            var primaryKeysCount = columns.Count(c => c.IsPrimaryKey);

            var schema = new TableSchema
            {
                TableName = _tableName,
                Columns = columns
            };

            // Автоматически запускаем логику формирования SQL-кусков (AllFields, SetClause и т.д.)
            schema.Initialize();

            // 🛡️ Защита от составного ключа: 
            // Если ключ один — записываем его имя (как обычно). 
            // Если ключей больше одного (как в Friends) — ставим null, чтобы генератор не сошел с ума, 
            // пытаясь сгенерировать UPDATE/DELETE по одиночному ID.
            schema.IdColumnName = primaryKeysCount == 1
                ? columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name
                : null;

            return schema;
        }
    }
    }


