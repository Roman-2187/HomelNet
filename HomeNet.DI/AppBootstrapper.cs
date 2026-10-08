using Dapper;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Builders;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.HomeNetOrm.Interfaces;
using HomeNetOrm.Repositories;
using HomeNetOrm.Sync.HomeNetOrm.Sync;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetServices.Diagnostics;
using HomeNetServices.Identity;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace HomeNet.DI
{
    public static class AppBootstrapper
    {
        private static IServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider => _serviceProvider
            ?? throw new InvalidOperationException("Контейнер еще не собран. Вызовите Build() или воспользуйтесь UI сборщиком.");

        /// <summary>
        /// 🔥 НАКИДЫВАЕТ БЭКЕНД-ПРОВОДА НА ЧЕРТЕЖ
        /// </summary>
        public static ServiceCollection CreateBackendCollection(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = new ServiceCollection();

            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<EventBusInspector>();
            services.AddSingleton<IEventBus, EventBus>();

            services.AddSingleton<IEventInspector>(provider =>
                provider.GetRequiredService<EventBusInspector>());

            // Логгер-неубивашка для моментального бэкапа
            services.AddSingleton<AppFileogger>(provider => new AppFileogger("App_debug.txt"));

            services.AddSingleton<ILogQueueManager>(provider =>
            {
                var eventBus = provider.GetRequiredService<IEventBus>();
                var uiManager = new LogQueueManager(eventBus, 1);
                var crashLogger = provider.GetRequiredService<AppFileogger>();

                provider.GetRequiredService<ILogger>().SetOutput((msg, level, ns) =>
                {
                    crashLogger.WriteImmediately(msg, level);
                    uiManager.WriteLog(msg, level, ns);
                });

                return uiManager;
            });

            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // =================================================================
            // 📡 ШАГ 1: РЕГИСТРАЦИЯ КОНКРЕТНЫХ КЛАССОВ ПОДКЛЮЧЕНИЙ
            // =================================================================

            // Локальный SQLite коннект регистрируем как конкретный тип, чтобы не было путаницы!
            services.AddSingleton<Microsoft.Data.Sqlite.SqliteConnection>(provider =>
                new Microsoft.Data.Sqlite.SqliteConnection(sqliteConn));

            // Центральный Postgres коннект
            services.AddSingleton<Npgsql.NpgsqlConnection>(provider =>
                new Npgsql.NpgsqlConnection(postgresConn));

            // Регистрация общего DbConnection для репозиториев (пусть по умолчанию указывает на SQLite!)
            services.AddSingleton<DbConnection>(provider =>
                provider.GetRequiredService<Microsoft.Data.Sqlite.SqliteConnection>());


            // =================================================================
            // 🏗️ ШАГ 2: РЕГИСТРАЦИЯ СТРОИТЕЛЕЙ ПОДКЛЮЧЕНИЙ (BUILDERS)
            // =================================================================

            // Наглухо скармливаем SQLite коннект в его билдер
            services.AddSingleton<IDbConnectionBuilder>(provider =>
                new SqliteConnectionBuilder(
                    provider.GetRequiredService<Microsoft.Data.Sqlite.SqliteConnection>(),
                    provider.GetRequiredService<ILogger>()));

            // Наглухо скармливаем NpgsqlConnection в Postgres билдер
            services.AddSingleton<PostgresConnectionBuilder>(provider =>
                new PostgresConnectionBuilder(
                    provider.GetRequiredService<Npgsql.NpgsqlConnection>(),
                    provider.GetRequiredService<ILogger>()));

            // =================================================================
            // 🎛️ ШАГ 3: РЕГИСТРАЦИЯ ХАБА КОНТЕКСТОВ СУБД (DbContextContainer)
            // =================================================================
            services.AddSingleton<IDbContextContainer, DbContextContainer>(provider =>
                new DbContextContainer(
                    provider.GetRequiredService<IDbConnectionBuilder>(),
                    provider.GetRequiredService<PostgresConnectionBuilder>(),
                    provider.GetRequiredService<IEventBus>(),
                    provider.GetRequiredService<ILogger>()));

            // =================================================================
            // 🧬 ШАГ 4: РЕГИСТРАЦИЯ НОВЫХ СПЕЦИАЛИЗИРОВАННЫХ ОРМ-ГЕНЕРАТОРОВ СУБД
            // =================================================================

            // Настраиваем SQLite ОРМ-адаптер для обслуживания локального репозитория
            services.AddSingleton<ISchemaAdapter>(provider =>
                new GenericSchemaAdapter(new SqliteProviderSpecification()));

            services.AddSingleton<IUserSqlGenerator, SqliteUserSqlGenerator>();
            services.AddSingleton<IMessageSqlGenerator, SqliteMessageSqlGenerator>();

            // =================================================================
            // 📦 ШАГ 5: РЕГИСТРАЦИЯ РЕПОЗИТОРИЕВ, СЕРВИСОВ И СИНХРОНИЗАТОРА
            // =================================================================

            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IMessageRepository, MessageRepository>();

            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IRegistrationService, RegistrationService>();
            services.AddSingleton<IAuthenticateService, AuthenticateService>();
            services.AddSingleton<IDeleteService, DeleteService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IFriendService, FriendService>();

            // Автономный ОРМ координатор синхронизации
            services.AddSingleton<ISiberNetSyncCoordinator, SiberNetSyncCoordinator>();

            // 3. Регистрация Вьюмоделей слоя Презентации
            services.AddSingleton<StatusBarViewModel>();
            services.AddSingleton<SystemButtonsViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<TerminalLogsViewModel>();
            services.AddSingleton<InspectorViewModel>();
            services.AddSingleton<AdminViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<TitleBarViewModel>();
            services.AddSingleton<SeedUsersViewModel>();
            services.AddSingleton<TableUsersViewModel>();

            services.AddSingleton<AdminNavigationManager>();
            services.AddSingleton<UserNavigationManager>();

            services.AddSingleton<NavigationStateManager>(provider =>
                provider.GetRequiredService<UserNavigationManager>());

            services.AddSingleton<UserViewModel>();
            services.AddSingleton<ContactsViewModel>();
            services.AddSingleton<MainViewModel>();

            return services;
        }

        /// <summary>
        /// Универсальный метод сборки бэкенда
        /// </summary>
        public static IServiceProvider Build(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = CreateBackendCollection(mode, postgresConn, sqliteConn);
            _serviceProvider = services.BuildServiceProvider();

            // 🔥 АВТОСТАРТ ИНИЦИАЛИЗАЦИИ И СИНХРОНИЗАЦИИ ПРИ СБОРКЕ КОНТЕЙНЕРА
            var dbContainer = _serviceProvider.GetRequiredService<IDbContextContainer>();
            var syncCoordinator = _serviceProvider.GetRequiredService<ISiberNetSyncCoordinator>();

            // Пинаем параллельный прогрев баз данных в бэкграунде
            Task.Run(async () =>
            {
                await dbContainer.InitializeAllDatabasesAsync();
                await syncCoordinator.StartAsync();
            });

            return _serviceProvider;
        }

        public static TGet GetViewModel<TGet>() where TGet : class
        {
            return ServiceProvider.GetRequiredService<TGet>();
        }

        public static void SetProvider(IServiceProvider provider)
        {
            _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
    }

    public class GuidTypeHandler : Dapper.SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        public override Guid Parse(object value)
        {
            if (value is Guid guid) return guid;
            if (value is string str && Guid.TryParse(str, out var parsedGuid))
            {
                return parsedGuid;
            }
            return Guid.Empty;
        }
    }
}
