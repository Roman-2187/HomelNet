using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetOrm.Interfaces;
using Dapper;
using HomeNetOrm.Infrastructure; 

namespace HomeNetOrm.Builders
{
    /// <summary>
    /// Центральный контейнер инфраструктуры СУБД Хаб (Оркестратор контекстов).
    /// </summary>
    public class DbContextContainer : IDbContextContainer
    {
        private readonly IDbConnectionBuilder _sqliteBuilder;
        private readonly IDbConnectionBuilder _postgresBuilder;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        public bool IsPostgresAvailable { get; private set; }

        public DbContextContainer(
            IDbConnectionBuilder sqliteBuilder,
            IDbConnectionBuilder postgresBuilder,
            IEventBus eventBus,
            ILogger logger)
        {
            _sqliteBuilder = sqliteBuilder ?? throw new ArgumentNullException(nameof(sqliteBuilder));
            _postgresBuilder = postgresBuilder ?? throw new ArgumentNullException(nameof(postgresBuilder));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAllDatabasesAsync()
        {
            // 🔥 Просто регистрируем твой хэндлер из папки инфраструктуры!
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            _logger.LogInfo("=== [СУБД ХАБ] ЗАПУСК ПАРАЛЛЕЛЬНОЙ ИНИЦИАЛИЗАЦИИ ИНФРАСТРУКТУРЫ ===");

            Task sqliteInitTask = InitializeSqliteChannelInternalAsync();
            Task postgresInitTask = InitializePostgresChannelInternalAsync();

            await Task.WhenAll(sqliteInitTask, postgresInitTask);

            _logger.LogInfo("=== [СУБД ХАБ] ИНИЦИАЛИЗАЦИЯ ИНФРАСТРУКТУРНЫХ КАНАЛОВ ЗАВЕРШЕНА ===");
            _eventBus.Publish(this, new IDbContextContainer.InfrastructureReady());
        }

        private async Task InitializeSqliteChannelInternalAsync()
        {
            try
            {
                await _sqliteBuilder.InitializeChannelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"[КАТАСТРОФА] Сбой авто-инициализации SQLite: {ex.Message}");
                throw;
            }
        }

        private async Task InitializePostgresChannelInternalAsync()
        {
            try
            {
                await _postgresBuilder.InitializeChannelAsync();
                IsPostgresAvailable = true;
            }
            catch (Exception ex)
            {
                IsPostgresAvailable = false;
                _logger.LogError($"[СУБД СТАТУС] Центральный PostgreSQL недоступен. Автономный режим. Причина: {ex.Message}");
            }
        }
    }
}
