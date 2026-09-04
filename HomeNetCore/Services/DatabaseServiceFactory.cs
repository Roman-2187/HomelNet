using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.DBProviders.Postgres;
using HomeNetCore.Data.DBProviders.Sqlite;
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
                    var pgSqlInit = new PostgresSchemaSqlInitializer(_logger, pgAdapter);

                    return (
                        pgConnection,
                        pgSqlInit,
                        new PostgresSchemaProvider(pgSqlInit, pgConnection,_logger),
                        pgAdapter
                    );

                default:
                    throw new ArgumentException($"Неподдерживаемый тип БД: {databaseType}", nameof(databaseType));
            }
        }

        /// <summary>
        /// 2. 🧙‍♂️ Магический штамповщик генераторов: возвращает КОНКРЕТНЫЙ КЛАСС напрямую!
        /// </summary>
        public ISqlGenerator<T> CreateSqlGenerator<T>(DatabaseType databaseType, ISchemaAdapter adapter) where T : class
        {
            return databaseType switch
            {
                DatabaseType.SQLite => new SqliteSqlGenerator<T>(adapter, _logger),

                // 🔥 УБИРАЕМ ЗАГЛУШКУ И СТАВИМ НАШ НАСТОЯЩИЙ ДЖЕНЕРИК ПОСТГРЕС-ГЕНЕРАТОР!
                DatabaseType.PostGreSQL => new PostgresSqlGenerator<T>(adapter, _logger),

                _ => throw new ArgumentOutOfRangeException(nameof(databaseType), $"Тип СУБД {databaseType} не поддерживается фабрикой.")
            };
        }


    }
}
