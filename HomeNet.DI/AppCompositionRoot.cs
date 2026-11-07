using HomeNet.DI.Modules;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.SignalRListeners;
using HomeNet.Synchronization;
using HomeNetCore.Enums;
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

        // Модули делаем internal, чтобы Bootstrapper мог достучаться до контейнеров бэкенда
        internal readonly CoreInfrastructureModule Infrastructure;
        public readonly DataAccessModule DataAccess;
        internal readonly BusinessServicesModule Services;
        internal readonly PresentationUiModule Ui;

        // 🔥 Прокидываем свойства для быстрого доступа
        public ICastomLogger Logger => Infrastructure.Logger;
        public IEventBus EventBus => Infrastructure.EventBus;
        public SyncEngine OrmEngine => DataAccess.OrmEngine;

        // 🔥 ВАЖНО: Теперь листенеры разделены по зонам ответственности!
        public UserSignalRListener UserListener => DataAccess.UserListener;
        public MessageSignalRListener MessageListener => DataAccess.MessageListener;

        private AppCompositionRoot(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            Infrastructure = new CoreInfrastructureModule();

            var schemaModule = new LocalMessengerSchemaModule();
            var registeredSchemas = schemaModule.GetSchemas();

            DataAccess = new DataAccessModule(
                sqliteConn,
                apiBaseUrl,
                registeredSchemas,
                Infrastructure.Logger,
                Infrastructure.LogQueueManager,
                Infrastructure.EventBus);

            Services = new BusinessServicesModule(
      DataAccess.UserRepository,          // Для чтения из кэша
      DataAccess.MessageRepository,       // Для чтения из кэша
      DataAccess.DeviceSettingsRepository,// Локальные настройки
      DataAccess.UserApiClient,           // 🔥 ДЛЯ РЕГИСТРАЦИИ И АУТЕНТИФИКАЦИИ
      DataAccess.MessageApiClient,        // 🔥 ДЛЯ ОТПРАВКИ СООБЩЕНИЙ
      Infrastructure.Logger,
      Infrastructure.EventBus);

            Ui = new PresentationUiModule(
                Infrastructure.EventBus,
                Infrastructure.Logger,
                Infrastructure.LogQueueManager,
                Services,
                Infrastructure.EventBusInspector
            );
        }

        /// <summary>
        /// Чистая фабричная сборка без скрытых фоновых тасок и подписок
        /// </summary>
        public static void Initialize(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            _instance = new AppCompositionRoot(mode, apiBaseUrl, sqliteConn);
        }

        public TGet GetViewModel<TGet>() where TGet : class
        {
            if (typeof(TGet) == typeof(TableUsersViewModel)) return (Ui.TableUsersVM as TGet)!;
            if (typeof(TGet) == typeof(UserViewModel)) return (Ui.UserVM as TGet)!;
            if (typeof(TGet) == typeof(MainViewModel)) return (Ui.MainVM as TGet)!;
            if (typeof(TGet) == typeof(ChatViewModel)) return (Ui.ChatVM as TGet)!;
            if (typeof(TGet) == typeof(StatusBarViewModel)) return (Ui.StatusBarVM as TGet)!;
            if (typeof(TGet) == typeof(SystemButtonsViewModel)) return (Ui.SystemButtonsVM as TGet)!;
            if (typeof(TGet) == typeof(RegistrationViewModel)) return (Ui.RegistrationVM as TGet)!;
            if (typeof(TGet) == typeof(AuthenticationViewModel)) return (Ui.AuthenticationVM as TGet)!;
            if (typeof(TGet) == typeof(TerminalLogsViewModel)) return (Ui.TerminalLogsVM as TGet)!;
            if (typeof(TGet) == typeof(InspectorViewModel)) return (Ui.InspectorVM as TGet)!;
            if (typeof(TGet) == typeof(AdminViewModel)) return (Ui.AdminVM as TGet)!;
            if (typeof(TGet) == typeof(DeleteUsersViewModel)) return (Ui.DeleteUsersVM as TGet)!;
            if (typeof(TGet) == typeof(TitleBarViewModel)) return (Ui.TitleBarVM as TGet)!;
            if (typeof(TGet) == typeof(SeedUsersViewModel)) return (Ui.SeedUsersVM as TGet)!;
            if (typeof(TGet) == typeof(ContactsViewModel)) return (Ui.ContactsVM as TGet)!;
            if (typeof(TGet) == typeof(DeviceSettingsViewModel)) return (Ui.DeviceSettingsVM as TGet)!;

            throw new ArgumentException($"Тип {typeof(TGet).Name} не найден в UI.");
        }
    }
}