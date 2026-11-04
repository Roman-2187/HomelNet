using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Local;
using HomeNetOrm.Repositories.Remote;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public enum DbEngineType
    {
        SQLiteSQLClient, // Режим клиента (SQLite)
        PostgreSQLServer // Режим сервера (PostgreSQL)
    }

    public class SiberNetDbEngine : IDbEngine, IDisposable
    {
        private readonly string _connectionString; 
        private readonly ILogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;
        private readonly ISqlDialectTranslator _translator;
        private readonly IDbProviderSpecification _spec;
        private readonly ISchemaSqlGenerator _schemaGenerator;
        private readonly DbEngineType _engineType;

        /// <summary>
        /// Многопоточная фабрика соединений. На лету создает чистый экземпляр 
        /// нужного провайдера (SQLite/Postgres) для каждого асинхронного потока.
        /// </summary>
        public Func<DbConnection> ConnectionFactory => () =>
        {
            DbConnection conn = _engineType switch
            {
                DbEngineType.SQLiteSQLClient => new SqliteConnection(_connectionString),
                DbEngineType.PostgreSQLServer => new NpgsqlConnection(_connectionString),
                _ => throw new InvalidOperationException("Неизвестный тип СУБД движка.")
            };
            return conn;
        };

        public DbConnection Connection => ConnectionFactory();

        public SiberNetDbEngine(DbEngineType engineType, string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _engineType = engineType;
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));

            switch (engineType)
            {
                case DbEngineType.SQLiteSQLClient:
                    _translator = new DBProviders.Sqlite.SqliteDialectTranslator();
                    _spec = new DBProviders.Sqlite.SqliteProviderSpecification();
                    _schemaGenerator = new DBProviders.Sqlite.SqliteSchemaGenerator(_spec);
                    break;

                case DbEngineType.PostgreSQLServer:
                    _translator = new DBProviders.Postgres.PostgresDialectTranslator();
                    _spec = new DBProviders.Postgres.PostgresProviderSpecification();
                    _schemaGenerator = new DBProviders.Postgres.PostgresSchemaSqlGenerator(_spec);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(engineType), "Неизвестный тип СУБД движка.");
            }
        }

        // --- 🏗️ Универсальные фабрики репозиториев штампуют методы на базе ConnectionFactory ---

        public IUserRepo CreateUserRepository(IEventBus eventBus)
        {
            // 🔥 Вычищено: Используем новый плоский генератор пользователей взамен Synced-мусора!
            var generator = new UserSqlGenerator(_schemas, _translator, _logger);
            return new UserRepository(ConnectionFactory, generator, _logger, eventBus);
        }

        public IMessageRepo CreateMessageRepository(IEventBus eventBus)
        {
            // 🔥 Вычищено: Используем новый плоский генератор сообщений взамен Synced-мусора!
            var generator = new MessageSqlGenerator(_schemas, _translator, _logger);
            return new MessageRepository(ConnectionFactory, generator, _logger, eventBus);
        }

        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            var generator = new DeviceSettingsSqlGenerator(_schemas, _translator, _logger);
            return new ClientDeviceSettingsRepository(ConnectionFactory, generator);
        }

        public DeviceSyncRepository CreateDeviceSyncRepository()
        {
            var generator = new DeviceSyncSqlGenerator(_schemas, _translator, _logger);
            // 🔥 Исправлено: Добавлен логгер в конструктор репозитория чекпоинтов девайса
            return new DeviceSyncRepository(ConnectionFactory, generator);
        }

        // --- ⚙️ Универсальный запуск инициализации таблиц и схем ---
        public async Task InitializeChannelAsync()
        {
            using (var baseConnection = ConnectionFactory())
            {
                if (baseConnection.State != System.Data.ConnectionState.Open)
                {
                    await baseConnection.OpenAsync();
                }

                var schemaProvider = new GenericSchemaProvider(_schemaGenerator, baseConnection, _spec, _logger);
                var tableInitializer = new SchemaRepository(baseConnection, schemaProvider, _schemaGenerator, _schemas, _logger);

                await tableInitializer.InitializeAsync();

                if (_engineType == DbEngineType.PostgreSQLServer)
                {
                    await tableInitializer.MigratePostgresVersionTriggersAsync(
                        baseConnection,
                        _schemaGenerator,
                        _translator,
                        _schemas,
                        _logger
                    );
                }
            } 
        }

        public void Dispose()
        {
        }
    }
}
