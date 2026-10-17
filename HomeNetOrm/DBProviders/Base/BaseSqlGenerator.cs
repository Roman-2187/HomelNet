using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// Абстрактное ядро ОРМ-генератора.
    /// Отвечает исключительно за автопилот поиска метаданных и snake_case трансформацию схем.
    /// </summary>
    internal abstract class BaseSqlGenerator<T> : ISqlGenerator<T> where T : class
    {
        public TableSchema FormattedTable { get; }
        protected readonly ISchemaAdapter Adapter;
        protected readonly ILogger Logger;

        // 🎯 КОНСТРУКТОР ТЕПЕРЬ ПРИНИМАЕТ СХЕМЫ ИЗВНЕ
        protected BaseSqlGenerator(ISchemaAdapter adapter, IEnumerable<TableSchema> availableSchemas, ILogger logger)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (availableSchemas == null) throw new ArgumentNullException(nameof(availableSchemas));

            string targetEntityName = typeof(T).Name;
            string cleanName = targetEntityName.Replace("Entity", "");

            // 🔍 Ищем схему строго в переданной извне коллекции!
            var rawTableSchema = availableSchemas
                .FirstOrDefault(s => s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                                     s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (rawTableSchema == null || string.IsNullOrEmpty(rawTableSchema.TableName))
            {
                Logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в переданном реестре.");
                throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует в реестре.");
            }

            FormattedTable = Adapter.ConvertToSnakeCaseSchema(rawTableSchema)
                ?? throw new InvalidOperationException($"Ошибка ОРМ-адаптера для схемы {targetEntityName}");
        }

        public abstract string GenerateInsert();
        public abstract string GenerateUpdate();
        public abstract string GenerateDelete();
        public abstract string GenerateSelectById();
        public abstract string GenerateSelectAll();

        public virtual string GenerateSelectUnsynced()
        {
            throw new NotImplementedException($"Метод GenerateSelectUnsynced не переопределен в конкретном провайдере для класса {typeof(T).Name}");
        }

        public virtual string GenerateUpdateMarkAsSynced()
        {
            throw new NotImplementedException($"Метод GenerateUpdateMarkAsSynced не переопределен в конкретном провайдере для класса {typeof(T).Name}");
        }

        public virtual string GenerateCountById()
        {
            throw new NotImplementedException($"Метод GenerateCountById не переопределен в конкретном провайдере для класса {typeof(T).Name}");
        }

        public virtual string GenerateDeleteById()
        {
            throw new NotImplementedException($"Метод GenerateDeleteById не переопределен в конкретном провайдере для класса {typeof(T).Name}");
        }

        public virtual object PrepareParameters(T entity)
        {
            return entity;
        }
    }
}
