using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres; // 🆕 Подключаем папку с транслятором
using HomeNetOrm.DBProviders.Postgres.@base;
using HomeNetOrm.DBProviders.Postgres.Base;
using HomeNetOrm.DBProviders.Postgres.SincedGenerators;
using HomeNetOrm.DBProviders.Postgres.SyncedGenerators;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure; // 🆕 Подключаем интерфейс ISqlDialectTranslator
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Remote;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public class SincedPostgresDbEngine : IDbEngine
    {
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly ILogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;

        // 🧙‍♂️ Единый источник правды для диалекта Postgres во всем движке!
        private readonly ISqlDialectTranslator _translator;

        public DbConnection Connection => _connection;

        public SincedPostgresDbEngine(string connectionString, IEnumerable<TableSchema> schemas, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            _connection = new Npgsql.NpgsqlConnection(connectionString);

            // 🎯 Создаем транслятор один раз при старте движка
            _translator = new PostgresDialectTranslator();
        }

        public IUserRepository CreateUserRepository()
        {
            // 🔥 Передаем _translator третьим параметром в наш обновленный генератор юзеров
            var generator = new SyncedPostgresUserSqlGenerator(_schemas, _translator, _logger);
            return new UserRepository(_connection, generator, _logger);
        }

        public IMessageRepository CreateMessageRepository()
        {
            // 🔥 Передаем _translator в генератор сообщений
            var generator = new SyncedPostgresMessageSqlGenerator(_schemas, _translator, _logger);
            return new MessageRepository(_connection, generator, _logger);
        }

        public IDeviceSyncRepository CreateDeviceSyncRepository()
        {
            // 🔥 Передаем _translator в генератор синхронизации девайсов
            var generator = new PostgresDeviceSyncSqlGenerator(_schemas, _translator, _logger);
            return new DeviceSyncRepository(_connection, generator);
        }

        // Внутри метода InitializeChannelAsync() в Системе Postgres...
        public async Task InitializeChannelAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var postgresSpec = new PostgresProviderSpecification();
            var postgresSchemaInit = new PostgresSchemaSqlGenerator(postgresSpec, _translator, _logger);

            // 1. Сначала прогоняем базовую инициализацию таблиц, как у тебя и было
            var postgresProvider = new GenericSchemaProvider(postgresSchemaInit, _connection, postgresSpec, _logger);
            var tableInitializer = new SchemaRepository(_connection, postgresProvider, postgresSchemaInit, _schemas, _logger);
            await tableInitializer.InitializeAsync();

            // 2. 🔥 А теперь бесшовно накатываем триггеры версии через методы расширения!
            _logger.LogInfo("[PostgreSQL] ⚡ Накатываем системную триггерную функцию авто-инкремента версий...");

            // Вызов идет через точку у интерфейса генератора схем!
            string functionSql = postgresSchemaInit.GenerateVersionFunctionSql(_translator);
            await _connection.ExecuteAsync(functionSql);

            foreach (var schema in _schemas)
            {
                // Проверяем, нужна ли этой таблице версионность (есть ли там колонка version)
                if (schema.Columns.Any(c => c.Name.Equals("version", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInfo($"[PostgreSQL] ⚡ Создаем триггер версии для таблицы {schema.TableName}...");
                    string triggerSql = postgresSchemaInit.GenerateVersionTriggerSql(schema, _translator);

                    try
                    {
                        await _connection.ExecuteAsync(triggerSql);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"[PostgreSQL] Триггер для {schema.TableName} уже существует: {ex.Message}");
                    }
                }
            }
        }

    }
}
