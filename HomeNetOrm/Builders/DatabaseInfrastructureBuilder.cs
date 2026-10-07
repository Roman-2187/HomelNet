using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders;
using HomeNetOrm.DBProviders.Extensions;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;

namespace HomeNetOrm.Builders
{
    public class DatabaseInfrastructureBuilder
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public DatabaseInfrastructureBuilder(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 1. Создаёт базовое подключение и общую инфраструктуру для инициализации всей БД.
        /// </summary>
        public (DbConnection connection, ISchemaSqlInitializer initializer,
                ISchemaProvider schemaProvider, ISchemaAdapter schemaAdapter)
            CreateCoreInfrastructure(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    var sqliteConnection = new SqliteConnection(_connectionString);

                    // 🔌 1. Просто забираем готовую, статически упакованную спецификацию из реестра
                    DbProviderSpecificationExtensions sqliteSpec = SqlQueriesRegistry.Sqlite;

                    // 2. Адаптер забирает готовую спеку
                    var sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);

                    // 3. Запросы вытягиваем прямо из свойств объекта спецификации
                    var sqliteSqlInit = new GenericSchemaSqlInitializer(_logger,
                        sqliteAdapter,
                        sqliteSpec.TableExistsQuery,
                        sqliteSpec.GetTableStructureQuery);

                    // 4. Провайдер забирает инициализатор, подключение, логгер и СВОЮ спеку
                    var sqliteProvider = new GenericSchemaProvider(
                        sqliteSqlInit,
                        sqliteConnection,
                        sqliteSpec,
                        _logger
                    );

                    return (sqliteConnection, sqliteSqlInit, sqliteProvider, sqliteAdapter);

                case DatabaseType.PostGreSQL:
                    var pgConnection = new NpgsqlConnection(_connectionString);

                    // 🐘 1. Забираем готовую спецификацию Postgres со всеми маппингами и индексами
                    DbProviderSpecificationExtensions pgSpec = SqlQueriesRegistry.Postgres;

                    // 2. Адаптер забирает спеку
                    var pgAdapter = new GenericSchemaAdapter(pgSpec);

                    // 3. Конфигурируем инициализатор без хардкода строк — всё лежит внутри pgSpec
                    var pgSqlInit = new GenericSchemaSqlInitializer(_logger, pgAdapter,
                        pgSpec.TableExistsQuery,
                        pgSpec.GetTableStructureQuery);

                    // 4. Провайдер забирает спеку в один параметр
                    var pgProvider = new GenericSchemaProvider(
                        pgSqlInit,
                        pgConnection,
                        pgSpec,
                        _logger
                    );

                    return (pgConnection, pgSqlInit, pgProvider, pgAdapter);

                default:
                    throw new ArgumentException($"Неподдерживаемый тип БД: {databaseType}", nameof(databaseType));
            }
        }

        /// <summary>
        /// 2. 🧙‍♂️ Магический штамповщик генераторов: возвращает КОНКРЕТНЫЙ КЛАСС напрямую!
        /// </summary>
        public ISqlGenerator<T> CreateSqlGenerator<T>(DatabaseType databaseType,
            ISchemaAdapter adapter) where T : class
        {
            return databaseType switch
            {
                DatabaseType.SQLite => new SqliteSqlGenerator<T>(adapter, _logger),
                DatabaseType.PostGreSQL => new PostgresSqlGenerator<T>(adapter, _logger),
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType),
                $"Тип СУБД {databaseType} не поддерживается фабрикой.")
            };
        }
    }
}
