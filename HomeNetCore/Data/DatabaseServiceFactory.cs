using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.DBProviders.Postgres;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.PostgreClasses;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using HomeNetCore.Data.SqliteClasses;
using HomeNetCore.Helpers;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;
using WpfHomeNet.Data.DBProviders.Postgres;




namespace HomeNetCore.Data
{

    public enum DatabaseType
    {
        SQLite,
        PostgreSQL
    }





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
        /// Создаёт набор сервисов для работы с указанной СУБД.
        /// </summary>
        /// <param name="databaseType">Тип СУБД (SQLite/PostgreSQL).</param>
        /// <param name="tableSchema">Схема таблицы для генерации SQL.</param>
        /// <returns>Кортеж из: соединения, инициализатора, провайдера схемы, адаптера и генератора SQL.</returns>
        public (DbConnection connection,
                 ISchemaSqlInitializer initializer,
                 ISchemaProvider schemaProvider,
                 ISchemaAdapter schemaAdapter,
                 IUserSqlGenerator userSqlGen)
            CreateServices(DatabaseType databaseType, TableSchema tableSchema)
        {
            if (tableSchema == null)
                throw new ArgumentNullException(nameof(tableSchema));

            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    var sqliteConnection = new SqliteConnection(_connectionString);
                    var sqliteAdapter = new SqliteSchemaAdapter();
                    var sqliteSqlInit = new SqliteSchemaSqlInit(_logger, sqliteAdapter);

                    return (
                        sqliteConnection,
                        sqliteSqlInit,
                        new SqliteGetSchemaProvider(sqliteSqlInit, sqliteConnection, _logger),
                        sqliteAdapter,
                        new SqliteUserSqlGen(tableSchema, sqliteAdapter, _logger)
                    );

                case DatabaseType.PostgreSQL:
                    var pgConnection = new NpgsqlConnection(_connectionString);
                    var pgAdapter = new PostgresSchemaAdapter();
                    var pgSqlInit = new PostgresSchemaSqlInit(_logger, pgAdapter);


                    return (
                        pgConnection,
                        pgSqlInit,
                        new PostgresSchemaProvider(pgSqlInit, pgConnection),
                        pgAdapter,
                        new PostgresUserSqlGen(tableSchema, pgAdapter, _logger)
                    );

                default:
                    throw new ArgumentException(
                        $"Неподдерживаемый тип БД: {databaseType}", nameof(databaseType));
            }
        }
    }


}
