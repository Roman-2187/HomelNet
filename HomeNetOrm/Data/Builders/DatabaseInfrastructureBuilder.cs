using HomeNetCore.Interfaces;
using HomeNetOrm.Data.DBProviders;
using HomeNetOrm.Data.DBProviders.Postgres;
using HomeNetOrm.Data.DBProviders.Sqlite;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;

namespace HomeNetOrm.Data.Builders
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
      

      public (DbConnection connection, ISchemaSqlInitializer initializer, ISchemaProvider schemaProvider, ISchemaAdapter schemaAdapter)
        CreateCoreInfrastructure(DatabaseType databaseType)
        {
            // Выносим парсер типов для SQLite/Postgres (старый метод MapType из провайдеров)
            Func<string, ColumnType> sqliteParser = dbType => dbType.ToLower() switch {
                "integer" => ColumnType.Integer,
                "text" => ColumnType.Varchar,
                "datetime" => ColumnType.DateTime,
                "boolean" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            };
            Func<string, ColumnType> postgresParser = dbType => dbType.ToLower() switch {
                "integer" or "serial" => ColumnType.Integer,
                "character varying" or "varchar" or "text" => ColumnType.Varchar,
                "timestamp without time zone" or "timestamp" => ColumnType.DateTime,
                "boolean" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            };

            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    var sqliteConnection = new SqliteConnection(_connectionString);

                    // Собираем универсальный адаптер на константах SQLite 🔌
                    var sqliteAdapter = new GenericSchemaAdapter(
                        SqlQueriesRegistry.Sqlite.MapToSqlType, sqliteParser,
                        SqlQueriesRegistry.Sqlite.NameIndex, SqlQueriesRegistry.Sqlite.TypeIndex, SqlQueriesRegistry.Sqlite.NullableIndex, SqlQueriesRegistry.Sqlite.PrimaryKeyIndex, SqlQueriesRegistry.Sqlite.ExtraInfoIndex
                    );

                    var sqliteSqlInit = new GenericSchemaSqlInitializer(_logger, sqliteAdapter, SqlQueriesRegistry.Sqlite.TableExists, SqlQueriesRegistry.Sqlite.GetTableStructure);

                    return (
                        sqliteConnection, sqliteSqlInit,
                        new GenericSchemaProvider(sqliteSqlInit, sqliteAdapter, sqliteConnection, _logger),
                        sqliteAdapter
                    );

                case DatabaseType.PostGreSQL:
                    var pgConnection = new NpgsqlConnection(_connectionString);

                    // Собираем универсальный адаптер на константах Postgres 🐘
                    var pgAdapter = new GenericSchemaAdapter(
                        SqlQueriesRegistry.Postgres.MapToSqlType, postgresParser,
                        SqlQueriesRegistry.Postgres.NameIndex, SqlQueriesRegistry.Postgres.TypeIndex, SqlQueriesRegistry.Postgres.NullableIndex, SqlQueriesRegistry.Postgres.PrimaryKeyIndex, SqlQueriesRegistry.Postgres.ExtraInfoIndex
                    );

                    var pgSqlInit = new GenericSchemaSqlInitializer(_logger, pgAdapter, SqlQueriesRegistry.Postgres.TableExists, SqlQueriesRegistry.Postgres.GetTableStructure);

                    return (
                        pgConnection, pgSqlInit,
                        new GenericSchemaProvider(pgSqlInit, pgAdapter, pgConnection, _logger),
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
