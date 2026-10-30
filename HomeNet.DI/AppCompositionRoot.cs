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
        // 🔥 Вытягиваем листенер из модуля данных по его настоящему имени!
        public HomeNet.Network.SignalRNotificationListener SignalRListener => _dataAccess.NotificationListener;

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

            logger.LogInfo("[App Старт] Принудительно будим синхронизатор...");
            try
            {
                // Стартуем движок в обход любых проверок пинга
                await _instance.OrmEngine.StartAsync();
                logger.LogInfo("[App Старт] ✅ OrmEngine запущен, пошла синхронизация!");
            }
            catch (Exception ex)
            {
                logger.LogError($"[App Старт] Ошибка: {ex.Message}");
            }
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
