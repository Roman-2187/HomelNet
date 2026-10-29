using HomeNet.DI.Modules;
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetPresentation.ViewModels.ClientViews;

namespace HomeNet.DI
{
    public class AppCompositionRoot
    {
        private static AppCompositionRoot? _instance;
        public static AppCompositionRoot Instance => _instance
            ?? throw new InvalidOperationException("Композиция не собрана. Вызовите AppCompositionRoot.Initialize(...)");

        private readonly CoreInfrastructureModule _infrastructure;
        private readonly DataAccessModule _dataAccess;
        private readonly BusinessServicesModule _services;
        private readonly PresentationUiModule _ui;

        public ILogger Logger => _infrastructure.Logger;
        public IEventBus EventBus => _infrastructure.EventBus;

        // 🔥 Публичный доступ к конкретному синхро-движку без кривых интерфейсов
        public SincEngine OrmEngine => _dataAccess.OrmEngine;

        private AppCompositionRoot(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            _infrastructure = new CoreInfrastructureModule();

            var schemaModule = new LocalMessengerSchemaModule();
            var registeredSchemas = schemaModule.GetSchemas();

            _dataAccess = new DataAccessModule(
                sqliteConn,
                apiBaseUrl,
                registeredSchemas,
                _infrastructure.Logger,
                _infrastructure.LogQueueManager,
                _infrastructure.EventBus);

            _services = new BusinessServicesModule(
                _dataAccess.UserRepository,
                _dataAccess.MessageRepository,
                _dataAccess.DeviceSettingsRepository,
                _infrastructure.Logger,
                _infrastructure.EventBus);

            _ui = new PresentationUiModule(
                _infrastructure.EventBus,
                _infrastructure.Logger,
                _infrastructure.LogQueueManager,
                _services,
                _infrastructure.EventBusInspector
            );
        }

        public static void Initialize(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            _instance = new AppCompositionRoot(mode, apiBaseUrl, sqliteConn);

            _instance._infrastructure.EventBus.SubscribeAsync<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);

            Task.Run(async () =>
            {
                await _instance._dataAccess.DbContextContainer.InitializeAllDatabasesAsync();
            });
        }

        private static async Task OnInfrastructureReady(IDbContextContainer.InfrastructureReady ev)
        {
            if (_instance == null) return;

            var logger = _instance._infrastructure.Logger;
            var dataAccess = _instance._dataAccess;

            logger.LogInfo("[App Старт] Локальные хранилища готовы. Проверяем статус бэкенд сервера...");

            using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) })
            {
                string pingUrl = $"{dataAccess.ApiBaseUrl.TrimEnd('/')}/api/ping";
                bool isServerUp = false;
                int attempts = 0;
                const int maxAttempts = 4;

                while (!isServerUp && attempts < maxAttempts)
                {
                    attempts++;
                    try
                    {
                        var response = await client.GetAsync(pingUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            isServerUp = true;
                            logger.LogInfo($"[App Старт] Бэкенд успешно ответил на {attempts}-й попытке. Сервер прогрет!");
                        }
                    }
                    catch
                    {
                        logger.LogWarning($"[App Старт] Сервер бэкенда еще занят или недоступен. Попытка {attempts}/{maxAttempts}...");

                        if (attempts < maxAttempts)
                        {
                            await Task.Delay(500);
                        }
                    }
                }

                if (!isServerUp)
                {
                    logger.LogWarning("[App Старт] Бэкенд не ответил за 4 попытки. Переходим в ОФЛАЙН режим работы.");
                }
            }

            if (dataAccess.NotificationListener != null)
            {
                try
                {
                    await dataAccess.NotificationListener.ConnectAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError($"[App Старт] Не удалось подключить логи бэкенда (офлайн): {ex.Message}");
                }
            }

            await Task.Delay(200);
            // 🔥 СТРОКА ХОЛОСТОГО ЗАПУСКА УДАЛЕНА ИЗ ПОДВАЛА. Теперь старт строго контролируемый!
        }

        public TGet GetViewModel<TGet>() where TGet : class
        {
            if (typeof(TGet) == typeof(TableUsersViewModel)) return (_ui.TableUsersVM as TGet)!;
            if (typeof(TGet) == typeof(UserViewModel)) return (_ui.UserVM as TGet)!;
            if (typeof(TGet) == typeof(MainViewModel)) return (_ui.MainVM as TGet)!;
            if (typeof(TGet) == typeof(ChatViewModel)) return (_ui.ChatVM as TGet)!;
            if (typeof(TGet) == typeof(StatusBarViewModel)) return (_ui.StatusBarVM as TGet)!;
            if (typeof(TGet) == typeof(SystemButtonsViewModel)) return (_ui.SystemButtonsVM as TGet)!;
            if (typeof(TGet) == typeof(RegistrationViewModel)) return (_ui.RegistrationVM as TGet)!;
            if (typeof(TGet) == typeof(AuthenticationViewModel)) return (_ui.AuthenticationVM as TGet)!;
            if (typeof(TGet) == typeof(TerminalLogsViewModel)) return (_ui.TerminalLogsVM as TGet)!;
            if (typeof(TGet) == typeof(InspectorViewModel)) return (_ui.InspectorVM as TGet)!;
            if (typeof(TGet) == typeof(AdminViewModel)) return (_ui.AdminVM as TGet)!;
            if (typeof(TGet) == typeof(DeleteUsersViewModel)) return (_ui.DeleteUsersVM as TGet)!;
            if (typeof(TGet) == typeof(TitleBarViewModel)) return (_ui.TitleBarVM as TGet)!;
            if (typeof(TGet) == typeof(SeedUsersViewModel)) return (_ui.SeedUsersVM as TGet)!;
            if (typeof(TGet) == typeof(ContactsViewModel)) return (_ui.ContactsVM as TGet)!;
            if (typeof(TGet) == typeof(DeviceSettingsViewModel)) return (_ui.DeviceSettingsVM as TGet)!;

            throw new ArgumentException($"Тип {typeof(TGet).Name} не найден в UI.");
        }
    }
}
