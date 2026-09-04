using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.DBProviders.Postgres;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.DBProviders.Sqlite.HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.PostgreClasses;
using HomeNetCore.Data.SqliteClasses;
using HomeNetCore.Enums;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;
using WpfHomeNet.Data.DBProviders.Postgres;

namespace HomeNetCore.Data
{
    public class DatabaseServiceFactory
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public DatabaseServiceFactory(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 1. Создаёт базовое подключение и общую инфраструктуру для инициализации всей БД.
        /// </summary>
        public (DbConnection connection,
                 ISchemaSqlInitializer initializer,
                 ISchemaProvider schemaProvider,
                 ISchemaAdapter schemaAdapter)
            CreateCoreInfrastructure(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    var sqliteConnection = new SqliteConnection(_connectionString);
                    var sqliteAdapter = new SqliteSchemaAdapter();
                    var sqliteSqlInit = new SchemaSqlInitializer(_logger, sqliteAdapter);

                    return (
                        sqliteConnection,
                        sqliteSqlInit,
                        new SqliteGetSchemaProvider(sqliteSqlInit, sqliteConnection, _logger),
                        sqliteAdapter
                    );

                case DatabaseType.PostGreSQL:
                    var pgConnection = new NpgsqlConnection(_connectionString);
                    var pgAdapter = new PostgresSchemaAdapter();
                    var pgSqlInit = new PostgresSchemaSqlInit(_logger, pgAdapter);

                    return (
                        pgConnection,
                        pgSqlInit,
                        new PostgresSchemaProvider(pgSqlInit, pgConnection),
                        pgAdapter
                    );

                default:
                    throw new ArgumentException($"Неподдерживаемый тип БД: {databaseType}", nameof(databaseType));
            }
        }

        /// <summary>
        /// 2. 🧙‍♂️ Магический штамповщик генераторов: возвращает КОНКРЕТНЫЙ КЛАСС напрямую!
        /// </summary>
        public SqliteSqlGenerator<T> CreateSqlGenerator<T>(DatabaseType databaseType, ISchemaAdapter adapter) where T : class
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    // Возвращаем сам класс! Никаких интерфейсов.
                    return new SqliteSqlGenerator<T>(adapter, _logger);

                case DatabaseType.PostGreSQL:
                    throw new NotImplementedException("PostgreSQL дженерик-генератор пока не реализован.");

                default:
                    throw new ArgumentException($"Неподдерживаемый тип БД для генератора: {databaseType}", nameof(databaseType));
            }
        }

    }
}
