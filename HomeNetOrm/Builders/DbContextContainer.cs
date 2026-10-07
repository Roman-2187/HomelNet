using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.DbTableInitializer;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeNetOrm.Builders
{
    /// <summary>
    /// Контейнер контекстов баз данных SiberNet. 
    /// Содержит параллельно запущенные и независимые каналы инфраструктуры для SQLite и Postgres.
    /// </summary>
    public class DbContextContainer : IAsyncDisposable
    {
        private readonly string _postgresConnectionString;
        private readonly string _sqliteConnectionString;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        // =================================================================
        // 🔋 ИНФРАСТРУКТУРНЫЙ КАНАЛ ЛОКАЛЬНОГО SQLITE (Живёт всегда)
        // =================================================================
        public DbConnection SqliteConnection { get; private set; } = null!;
        public ISqlGenerator<UserEntity> SqliteUserSqlGen { get; private set; } = null!;
        public ISqlGenerator<MessageEntity> SqliteMessageSqlGen { get; private set; } = null!;
        public ISqlGenerator<FriendEntity> SqliteFriendSqlGen { get; private set; } = null!;

        // =================================================================
        // 🐘 ИНФРАСТРУКТУРНЫЙ КАНАЛ ЦЕНТРАЛЬНОГО POSTGRESQL (Онлайн)
        // =================================================================
        public DbConnection PostgresConnection { get; private set; } = null!;
        public ISqlGenerator<UserEntity> PostgresUserSqlGen { get; private set; } = null!;
        public ISqlGenerator<MessageEntity> PostgresMessageSqlGen { get; private set; } = null!;
        public ISqlGenerator<FriendEntity> PostgresFriendSqlGen { get; private set; } = null!;

        // Состояния подключения к базам
        public bool IsPostgresAvailable { get; private set; }

        public DbContextContainer(string postgresConn, string sqliteConn, IEventBus eventBus, ILogger logger)
        {
            SQLitePCL.Batteries.Init();

            _postgresConnectionString = postgresConn ?? throw new ArgumentNullException(nameof(postgresConn));
            _sqliteConnectionString = sqliteConn ?? throw new ArgumentNullException(nameof(sqliteConn));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Главная точка входа бутстраппера: инициализирует и поднимает ОБЕ базы данных.
        /// </summary>
        public async Task InitializeAllDatabasesAsync()
        {
            _logger.LogInfo("=== [СУБД ХАБ] ЗАПУСК ПАРАЛЛЕЛЬНОЙ ИНИЦИАЛИЗАЦИИ ИНФРАСТРУКТУРЫ ===");

            // 1. Инициализируем локальный SQLite (Должен взлетать при любых раскладах)
            try
            {
                await InitializeSqliteChannelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"[КАТАСТРОФА] Сбой инициализации локального канала [SQLITE]: {ex.Message}");
                throw;
            }

            // 2. Инициализируем центральный Postgres (Если упадет — фиксируем статус офлайна)
            try
            {
                await InitializePostgresChannelAsync();
                IsPostgresAvailable = true;
            }
            catch (Exception ex)
            {
                IsPostgresAvailable = false;
                _logger.LogError($"[СУБД СТАТУС] Центральный [POSTGRES] недоступен при старте. " +
                                 $"Приложение переведено в локальный автономный режим. Причина: {ex.Message}");
            }

            _logger.LogInfo("=== [СУБД ХАБ] ИНИЦИАЛИЗАЦИЯ ИНФРАСТРУКТУРНЫХ КАНАЛОВ ЗАВЕРШЕНА ===");
        }

        private async Task InitializeSqliteChannelAsync()
        {
            _logger.LogInfo("[SQLITE] Запуск развёртывания локальной инфраструктуры...");
            var builder = new DatabaseInfrastructureBuilder(_sqliteConnectionString, _logger);

            var (connection, sqlInit, schemaProvider, schemaAdapter) =
                builder.CreateCoreInfrastructure(DatabaseType.SQLite);

            SqliteConnection = connection;
            if (SqliteConnection.State != ConnectionState.Open) await SqliteConnection.OpenAsync();

            // 🔥 Передаем чёткую метку канала в логгер инициализатора, чтобы не путать таблицы
            _logger.LogInfo("[SQLITE] Прогрев и накат локальных схем таблиц...");
            var dbInitializer = new DBInitializer(SqliteConnection, schemaProvider, schemaAdapter, sqlInit, sqlInit, _eventBus, _logger);
            await dbInitializer.InitializeAsync();

            SqliteUserSqlGen = builder.CreateSqlGenerator<UserEntity>(DatabaseType.SQLite, schemaAdapter);
            SqliteMessageSqlGen = builder.CreateSqlGenerator<MessageEntity>(DatabaseType.SQLite, schemaAdapter);
            SqliteFriendSqlGen = builder.CreateSqlGenerator<FriendEntity>(DatabaseType.SQLite, schemaAdapter);

            _logger.LogInfo("✅ [SQLITE] Локальный канал успешно запущен и синхронизирован с ORM-схемами.");
        }

        private async Task InitializePostgresChannelAsync()
        {
            _logger.LogInfo("[POSTGRES] Запуск развёртывания центральной инфраструктуры...");
            var builder = new DatabaseInfrastructureBuilder(_postgresConnectionString, _logger);

            var (connection, sqlInit, schemaProvider, schemaAdapter) =
                builder.CreateCoreInfrastructure(DatabaseType.PostGreSQL);

            PostgresConnection = connection;
            if (PostgresConnection.State != ConnectionState.Open) await PostgresConnection.OpenAsync();

            // 🔥 Передаем чёткую метку канала в логгер инициализатора Postgres
            _logger.LogInfo("[POSTGRES] Прогрев и сверка схем таблиц на сервере...");
            var dbInitializer = new DBInitializer(PostgresConnection, schemaProvider, schemaAdapter, sqlInit, sqlInit, _eventBus, _logger);
            await dbInitializer.InitializeAsync();

            PostgresUserSqlGen = builder.CreateSqlGenerator<UserEntity>(DatabaseType.PostGreSQL, schemaAdapter);
            PostgresMessageSqlGen = builder.CreateSqlGenerator<MessageEntity>(DatabaseType.PostGreSQL, schemaAdapter);
            PostgresFriendSqlGen = builder.CreateSqlGenerator<FriendEntity>(DatabaseType.PostGreSQL, schemaAdapter);

            _logger.LogInfo("✅ [POSTGRES] Центральный канал успешно подключен, схемы проверены.");
        }

        /// <summary>
        /// Метод быстрой проверки связи с PostgreSQL для репозиториев и координатора.
        /// </summary>
        public async Task<bool> CheckPostgresConnectionAsync()
        {
            if (PostgresConnection == null) return false;

            try
            {
                if (PostgresConnection.State != ConnectionState.Open)
                {
                    await PostgresConnection.OpenAsync();
                }

                using var cmd = PostgresConnection.CreateCommand();
                cmd.CommandText = "SELECT 1;";
                await cmd.ExecuteScalarAsync();

                if (!IsPostgresAvailable)
                {
                    _logger.LogInfo("⚡ [СУБД СТАТУС] Связь с [POSTGRES] успешно восстановлена!");
                }
                IsPostgresAvailable = true;
                return true;
            }
            catch
            {
                if (IsPostgresAvailable)
                {
                    _logger.LogWarning("⚠️ [СУБД СТАТУС] Потеряно соединение с центральным [POSTGRES]! Уходим в офлайн.");
                }
                IsPostgresAvailable = false;
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInfo("[СУБД ХАБ] Выгрузка и безопасное закрытие всех открытых каналов баз данных...");
            if (SqliteConnection != null) await SqliteConnection.DisposeAsync();
            if (PostgresConnection != null) await PostgresConnection.DisposeAsync();
        }
    }
}
