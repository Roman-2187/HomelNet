using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetOrm.Interfaces.Builders;
using System;
using System.Threading.Tasks;

namespace HomeNetOrm.Infrastructure
{
    public class DbContextContainer(
        IDbEngine sqliteEngine,       // 🔌 Передали упакованный SQLite
        IDbEngine postgresEngine,     // 🔌 Передали упакованный Postgres
        IEventBus eventBus,
        ILogger logger) : IDbContextContainer
    {
        private readonly IDbEngine _sqliteEngine = sqliteEngine ?? throw new ArgumentNullException(nameof(sqliteEngine));
        private readonly IDbEngine _postgresEngine = postgresEngine ?? throw new ArgumentNullException(nameof(postgresEngine));
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public bool IsPostgresAvailable { get; private set; }

        public async Task InitializeAllDatabasesAsync()
        {
            _logger.LogInfo("=== [СУБД ХАБ] ЗАПУСК ПАРАЛЛЕЛЬНОЙ ИНИЦИАЛИЗАЦИИ ДВИЖКОВ ===");

            // Движки сами знают, как открыть свои внутренние подключения!
            Task sqliteInitTask = _sqliteEngine.InitializeChannelAsync();
            Task postgresInitTask = InitializePostgresChannelInternalAsync();

            await Task.WhenAll(sqliteInitTask, postgresInitTask);

            _logger.LogInfo("=== [СУБД ХАБ] ИНИЦИАЛИЗАЦИЯ ДВИЖКОВ ЗАВЕРШЕНА ===");
            _eventBus.Publish(this, new IDbContextContainer.InfrastructureReady());
        }

        private async Task InitializePostgresChannelInternalAsync()
        {
            try
            {
                await _postgresEngine.InitializeChannelAsync();
                IsPostgresAvailable = true;
            }
            catch (Exception ex)
            {
                IsPostgresAvailable = false;
                _logger.LogError($"[СУБД СТАТУС] Центральный PostgreSQL недоступен: {ex.Message}");
            }
        }
    }
}
