using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
using HomeNetOrm.Schemes;
using System;
using System.Linq;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Абстрактное ядро ОРМ-генератора.
    /// Отвечает исключительно за автопилот поиска метаданных и snake_case трансформацию схем.
    /// </summary>
    public abstract class BaseSqlGenerator<T> : ISqlGenerator<T> where T : class
    {
        public TableSchema FormattedTable { get; }
        protected readonly ISchemaAdapter Adapter;
        protected readonly ILogger Logger;

        protected BaseSqlGenerator(ISchemaAdapter adapter, ILogger logger)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            var rawTableSchema = SchemaRegistry.GetAllSchemas()
                .FirstOrDefault(s => s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                                     s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (rawTableSchema == null || string.IsNullOrEmpty(rawTableSchema.TableName))
            {
                Logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в общем реестре.");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует в реестре.");
            }

            FormattedTable = Adapter.ConvertToSnakeCaseSchema(rawTableSchema)
                ?? throw new InvalidOperationException($"Ошибка ОРМ-адаптера для схемы {targetEntityName}");
        }

        // 🔥 Жесткие абстрактные CRUD-контракты. Никакой мешанины диалектов в базе!
        public abstract string GenerateInsert();
        public abstract string GenerateUpdate();
        public abstract string GenerateDelete();
        public abstract string GenerateSelectById();
        public abstract string GenerateSelectAll();
    }
}
