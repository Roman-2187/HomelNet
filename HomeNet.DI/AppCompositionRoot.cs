using HomeNet.DI.Modules;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Network;
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

        private static bool _isSyncStarted = false; // Защита от двойного старта

        public SignalRNotificationListener SignalRListener => _dataAccess.NotificationListener;
        public ILogger Logger => _infrastructure.Logger;
        public IEventBus EventBus => _infrastructure.EventBus;
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

            // ПОДПИСКИ АВТОБУСА БЕЗ ЛЯМБД — строго на чистые методы
            _instance._infrastructure.EventBus.SubscribeAsync<ServerBecameReadyEvent>(OnServerBecameReady);
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

            logger.LogInfo("[App Старт] Локальные СУБД инициализированы. Поднимаем сокет-листенер...");

            try
            {
                await _instance.SignalRListener.ConnectAsync();

                // ВЗВОДИМ АВТОНОМНЫЙ ТАЙМЕР НА 8 СЕКУНД
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(8));

                    if (!_isSyncStarted)
                    {
                        logger.LogWarning("[SiberNet Engine] ⏳ Превышено время ожидания сервера (8 сек). Переходим в Автономный режим (Offline Mode).");

                        // 🔥 СИГНАЛИЗИРУЕМ СТАТУС-БАРУ: СЕРВЕР НЕ ОТВЕТИЛ, МЫ ОФЛАЙН
                        _instance._infrastructure.EventBus.Publish(_instance, new NetworkStatusChanged(false));
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"[App Старт] Ошибка старта сокет-клиента: {ex.Message}");
                // В случае падения сокета сразу гасим индикатор в офлайн
                _instance._infrastructure.EventBus.Publish(_instance, new NetworkStatusChanged(false));
            }
        }

        private static async Task OnServerBecameReady(ServerBecameReadyEvent ev)
        {
            if (_instance == null || _isSyncStarted) return;
            _isSyncStarted = true;

            var logger = _instance._infrastructure.Logger;
            logger.LogInfo("[SiberNet Engine] Бортовой компьютер получил сигнал готовности! Будим репликацию...");

            // 🔥 СИГНАЛИЗИРУЕМ СТАТУС-БАРУ: РУКОПОЖАТИЕ ПРОШЛО, СЕТЬ КИПИТ!
            _instance._infrastructure.EventBus.Publish(_instance, new NetworkStatusChanged(true));

            try
            {
                await _instance.OrmEngine.StartAsync();
                logger.LogInfo("[App Старт] ✅ Движок OrmEngine успешно запущен, пошла синхронизация данных!");
            }
            catch (Exception ex)
            {
                logger.LogError($"[App Старт] Критическая ошибка движка репликации: {ex.Message}");

                _instance.EventBus.Publish(_instance, new NetworkStatusChanged(false));
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
