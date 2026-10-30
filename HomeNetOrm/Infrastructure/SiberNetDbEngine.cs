using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Generators.Sinc;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
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
        LocalClient, // Режим клиента (SQLite)
        RemoteServer // Режим сервера (PostgreSQL)
    }

    public class SiberNetDbEngine : IDbEngine, IDisposable
    {
        private readonly string _connectionString; // 🔥 Храним строку подключения вместо долгоживущего объекта коннекшена
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
                DbEngineType.LocalClient => new SqliteConnection(_connectionString),
                DbEngineType.RemoteServer => new NpgsqlConnection(_connectionString),
                _ => throw new InvalidOperationException("Неизвестный тип СУБД движка.")
            };
            return conn;
        };

        // Оставляем для обратной совместимости с интерфейсом IDbEngine, выдавая свежий коннекшен
        public DbConnection Connection => ConnectionFactory();

        public SiberNetDbEngine(DbEngineType engineType, string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _engineType = engineType;
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));

            // 🧙‍♂️ Настройка стратегии трансляции ОДИН РАЗ в конструкторе. Никаких рантайм if-else в фабриках!
            switch (engineType)
            {
                case DbEngineType.LocalClient:
                    _translator = new DBProviders.Sqlite.SqliteDialectTranslator();
                    _spec = new DBProviders.Sqlite.SqliteProviderSpecification();
                    _schemaGenerator = new DBProviders.Sqlite.SqliteSchemaGenerator(_spec);
                    break;

                case DbEngineType.RemoteServer:
                    _translator = new DBProviders.Postgres.PostgresDialectTranslator();
                    _spec = new DBProviders.Postgres.PostgresProviderSpecification();
                    _schemaGenerator = new DBProviders.Postgres.PostgresSchemaSqlGenerator(_spec);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(engineType), "Неизвестный тип СУБД движка.");
            }
        }

        // --- 🏗️ Универсальные фабрики репозиториев штампуют методы на базе ConnectionFactory ---

        public IUserRepository CreateUserRepository(IEventBus eventBus)
        {
            var generator = new SyncedUserSqlGenerator(_schemas, _translator, _logger);
            // Прокидываем лямбду ConnectionFactory вместо старого одиночного _connection
            return new UserRepository(ConnectionFactory, generator, _logger, eventBus);
        }

        public IMessageRepository CreateMessageRepository(IEventBus eventBus)
        {
            var generator = new SyncedMessageSqlGenerator(_schemas, _translator, _logger);
            return new MessageRepository(ConnectionFactory, generator, _logger, eventBus);
        }

        public ClientDeviceSettingsRepository CreateDeviceSettingsRepository()
        {
            var generator = new DeviceSettingsSqlGenerator(_schemas, _translator, _logger);
            return new ClientDeviceSettingsRepository(ConnectionFactory, generator);
        }

        public IDeviceSyncRepository CreateDeviceSyncRepository()
        {
            var generator = new DeviceSyncSqlGenerator(_schemas, _translator, _logger);
            return new DeviceSyncRepository(ConnectionFactory, generator);
        }

        // --- ⚙️ Универсальный запуск инициализации таблиц и схем ---
        public async Task InitializeChannelAsync()
        {
            // Для первоначальной миграции и сверки таблиц поднимаем один временный коннекшен
            using (var baseConnection = ConnectionFactory())
            {
                if (baseConnection.State != System.Data.ConnectionState.Open)
                {
                    await baseConnection.OpenAsync();
                }

                var schemaProvider = new GenericSchemaProvider(_schemaGenerator, baseConnection, _spec, _logger);

                // Создаем инициализатор таблиц ("табле криатер")
                var tableInitializer = new SchemaRepository(baseConnection, schemaProvider, _schemaGenerator, _schemas, _logger);

                // 1. Запуск базового наката таблиц и сверки колонок (SQLite / Postgres)
                await tableInitializer.InitializeAsync();

                // 2. 🔥 КРАСОТА: Вызываем наше новое расширение прямо через точку от инстанса tableInitializer!
                if (_engineType == DbEngineType.RemoteServer)
                {
                    await tableInitializer.MigratePostgresVersionTriggersAsync(
                        baseConnection,
                        _schemaGenerator,
                        _translator,
                        _schemas,
                        _logger
                    );
                }
            } // Инициализационный коннекшен гарантированно закрывается и уничтожается здесь
        }

        public void Dispose()
        {
            // Больше не нужно принудительно диспозить глобальный коннекшен,
            // так как репозитории будут сами управлять своими соединениями через using блоки
        }
    }
}
