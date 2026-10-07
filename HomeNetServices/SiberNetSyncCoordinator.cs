using Dapper;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using System;
using System.Data;
using System.Threading.Tasks;

namespace HomeNetServices.Synchronization
{
    /// <summary>
    /// Координатор синхронизации SiberNet. 
    /// Отвечает за двустороннее "заглядывание" баз при старте и активацию реалтайм-моста через EventBus.
    /// </summary>
    public class SiberNetSyncCoordinator : IDisposable
    {
        private readonly DbContextContainer _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        private bool _isSqliteReady = false;
        private bool _isPostgresReady = false;
        private bool _isSyncExecuted = false;

        public SiberNetSyncCoordinator(DbContextContainer context, IEventBus eventBus, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Ловим сигналы готовности структур таблиц от DBInitializer
            _eventBus.Subscribe<ISchemaSqlInitializer.DatabaseReady>(OnDatabaseReady);
        }

        private int _readySignalsCount = 0; // Счётчик прилетевших сигналов готовности

        private async void OnDatabaseReady(ISchemaSqlInitializer.DatabaseReady msg)
        {
            _readySignalsCount++;

            _logger.LogInfo($"[СИНХРОНИЗАТОР] Получен сигнал готовности структуры БД (Сигнал №{_readySignalsCount}).");

            // 🎯 ЖЕСТКИЙ ФИКС: Ждём Сигнал №2 И проверяем, что генераторы Постгреса успели успешно создаться!
            if (_readySignalsCount == 2 && _context.IsPostgresAvailable && _context.PostgresUserSqlGen != null)
            {
                if (!_isSyncExecuted)
                {
                    _isSyncExecuted = true;
                    _isSqliteReady = true;
                    _isPostgresReady = true;
                    await RunSyncProcessAsync();
                }
            }
            // Если прилетел только один сигнал (например, от SQLite), а Постгрес реально лежит
            else if (_readySignalsCount == 1)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // Даём фору в 2 секунды

                    // Если за 2 секунды второй сигнал так и не пришел или Постгрес поврежден
                    if (_readySignalsCount == 1 && !_isSyncExecuted)
                    {
                        _isSyncExecuted = true;
                        _isSqliteReady = true;
                        _logger.LogWarning("[СИНХРОНИЗАТОР] PostgreSQL недоступен или его инициализация сорвалась. Работаем в автономном режиме SQLite.");
                        _eventBus.Publish(this, new SyncCompletedEvent());
                    }
                });
            }
        }



        private async Task RunSyncProcessAsync()
        {
            _logger.LogInfo("=== [СИНХРОНИЗАТОР] ЗАПУСК ДВУСТОРОННЕЙ СВЕРКИ ДАННЫХ ===");

            try
            {
                // Проверяем, пуста ли локальная база
                int localUsersCount = await _context.SqliteConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users;");

                if (localUsersCount == 0)
                {
                    // 🔥 ТВОЙ СЦЕНАРИЙ: SQLite пуст -> Выкачиваем всё из непустого Postgres
                    await InitialPullFromPostgresAsync();
                }
                else
                {
                    // Обычный сценарий: Честная двусторонняя сверка (Push офлайна -> Pull онлайна)
                    await ExecuteTwoWaySyncAsync();
                }

                _logger.LogInfo("=== [СИНХРОНИЗАТОР] СВЕРКА БАЗ УСПЕШНО ЗАВЕРШЕНА ===");

                // Стреляем маркерным событием в шину, сообщая вьюмоделям, что данные слиты и можно обновлять UI
                _eventBus.Publish(this, new SyncCompletedEvent());
            }
            catch (Exception ex)
            {
                _logger.LogError($"[СИНХРОНИЗАТОР] [КРИТ] Сбой синхронизации: {ex.Message}");

                // Аварийный парашют "Last Hope" — сбрасываем дамп ошибки на диск
                await System.IO.File.AppendAllTextAsync("lasthope_sync_fail.txt",
                    $"[{DateTime.UtcNow}] Ошибка стартового слияния: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Сценарий "Первый старт": переливаем данные из живого Postgres в пустой SQLite
        /// </summary>
        private async Task InitialPullFromPostgresAsync()
        {
            _logger.LogInfo("[СИНХРОНИЗАТОР] Первичная заливка локального кэша из PostgreSQL...");

            // 1. Копируем пользователей
            var remoteUsers = await _context.PostgresConnection.QueryAsync<UserEntity>("SELECT * FROM users;");
            int uCount = 0;
            string userInsertSql = _context.SqliteUserSqlGen.GenerateInsert();

            foreach (var user in remoteUsers)
            {
                user.IsSynced = 1; // Данные уже на сервере
                await _context.SqliteConnection.ExecuteAsync(userInsertSql, user);
                uCount++;
            }
            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перенесено учетных записей: {uCount}");

            // 2. Копируем сообщения чатов
            var remoteMessages = await _context.PostgresConnection.QueryAsync<MessageEntity>("SELECT * FROM messages;");
            int mCount = 0;
            string msgInsertSql = _context.SqliteMessageSqlGen.GenerateInsert();

            foreach (var msg in remoteMessages)
            {
                msg.IsSynced = 1;
                await _context.SqliteConnection.ExecuteAsync(msgInsertSql, msg);
                mCount++;
            }
            _logger.LogInfo($"[СИНХРОНИЗАТОР] Перенесено сообщений истории: {mCount}");
        }

        /// <summary>
        /// Сценарий "Повторный старт": классический Push/Pull на основе GUID и таймштампов
        /// </summary>
        private async Task ExecuteTwoWaySyncAsync()
        {
            // Шаг А: PUSH (Выгружаем локальный офлайн в Postgres)
            _logger.LogInfo("[СИНХРОНИЗАТОР] Шаг А: Выгрузка локальных офлайн-изменений (is_synced = 0)...");
            var unsyncedUsers = await _context.SqliteConnection.QueryAsync<UserEntity>("SELECT * FROM users WHERE is_synced = 0;");
            string pgUserUpsert = _context.PostgresUserSqlGen.GenerateInsert() +
                " ON CONFLICT (id) DO UPDATE SET first_name = EXCLUDED.first_name, last_name = EXCLUDED.last_name, updated_at = EXCLUDED.updated_at;";

            foreach (var user in unsyncedUsers)
            {
                await _context.PostgresConnection.ExecuteAsync(pgUserUpsert, user);
                await _context.SqliteConnection.ExecuteAsync("UPDATE users SET is_synced = 1 WHERE id = @Id", new { Id = user.Id });
            }

            // Шаг Б: PULL (Заглядываем в Postgres и забираем то, чего у нас нет)
            _logger.LogInfo("[СИНХРОНИЗАТОР] Шаг Б: Стягивание свежих обновлений по меткам времени...");
            var lastLocalUpdate = await _context.SqliteConnection.ExecuteScalarAsync<DateTimeOffset?>("SELECT MAX(updated_at) FROM users;") ?? DateTimeOffset.MinValue;

            var remoteChanges = await _context.PostgresConnection.QueryAsync<UserEntity>("SELECT * FROM users WHERE updated_at > @LastLocal;", new { LastLocal = lastLocalUpdate });
            string sqliteUserUpsert = _context.SqliteUserSqlGen.GenerateInsert() +
                " ON CONFLICT (id) DO UPDATE SET first_name = excluded.first_name, last_name = excluded.last_name, updated_at = excluded.updated_at, is_synced = 1;";

            foreach (var user in remoteChanges)
            {
                await _context.SqliteConnection.ExecuteAsync(sqliteUserUpsert, user);
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ISchemaSqlInitializer.DatabaseReady>(OnDatabaseReady);
        }
    }

    /// <summary>
    /// Маркерное событие для шины: Слияние баз окончено, UI может безопасно перечитывать списки.
    /// </summary>
    public record SyncCompletedEvent;
}
