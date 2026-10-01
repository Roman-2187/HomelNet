using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Models;
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

                    // 🔌 1. Запаковываем все метаданные SQLite в одну строгую модель конфигурации
                    var sqliteSpec = new DbProviderSpecification(
                        SqlQueriesRegistry.Sqlite.MapToSqlType,
                        SqlQueriesRegistry.Sqlite.ParsePropertyType,
                        SqlQueriesRegistry.Sqlite.NameIndex,
                        SqlQueriesRegistry.Sqlite.TypeIndex,
                        SqlQueriesRegistry.Sqlite.NullableIndex,
                        SqlQueriesRegistry.Sqlite.PrimaryKeyIndex,
                        SqlQueriesRegistry.Sqlite.ExtraInfoIndex
                    );

                    // 2. Адаптер теперь забирает только спеку
                    var sqliteAdapter = new GenericSchemaAdapter(sqliteSpec);

                    var sqliteSqlInit = new GenericSchemaSqlInitializer(_logger,
                        sqliteAdapter, SqlQueriesRegistry.Sqlite.TableExists,
                        SqlQueriesRegistry.Sqlite.GetTableStructure);

                    // 3. Провайдер забирает инициализатор, подключение, логгер и СВОЮ спеку (без адаптера!)
                    var sqliteProvider = new GenericSchemaProvider(
                        sqliteSqlInit,
                        sqliteConnection,
                        sqliteSpec,
                        _logger
                    );

                    return (sqliteConnection, sqliteSqlInit, sqliteProvider, sqliteAdapter);

                case DatabaseType.PostGreSQL:
                    var pgConnection = new NpgsqlConnection(_connectionString);

                    // 🐘 1. Запаковываем все метаданные Postgres в спецификацию
                    var pgSpec = new DbProviderSpecification(
                        SqlQueriesRegistry.Postgres.MapToSqlType,
                        SqlQueriesRegistry.Postgres.ParsePropertyType,
                        SqlQueriesRegistry.Postgres.NameIndex,
                        SqlQueriesRegistry.Postgres.TypeIndex,
                        SqlQueriesRegistry.Postgres.NullableIndex,
                        SqlQueriesRegistry.Postgres.PrimaryKeyIndex,
                        SqlQueriesRegistry.Postgres.ExtraInfoIndex
                    );

                    // 2. Адаптер забирает спеку
                    var pgAdapter = new GenericSchemaAdapter(pgSpec);

                    var pgSqlInit = new GenericSchemaSqlInitializer(_logger, pgAdapter,
                        SqlQueriesRegistry.Postgres.TableExists,
                        SqlQueriesRegistry.Postgres.GetTableStructure);

                    // 3. Провайдер честно забирает свою спеку
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
