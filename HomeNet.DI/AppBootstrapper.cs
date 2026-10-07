using Dapper;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Builders;
using HomeNetOrm.Repositories;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetServices.Diagnostics;
using HomeNetServices.Identity;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
using HomeNetServices.Synchronization;
using Microsoft.Extensions.DependencyInjection;

namespace HomeNet.DI
{
    public static class AppBootstrapper
    {
        private static IServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider => _serviceProvider
            ?? throw new InvalidOperationException("Контейнер еще не собран. Вызовите Build() или воспользуйтесь UI сборщиком.");

        /// <summary>
        /// 🔥 ШАГ А: Накидывает бэкенд-провода на чертеж (без сборки контейнера!)
        /// Используется в WPF для дальнейшего расширения UI-сервисами.
        /// </summary>
        public static ServiceCollection CreateBackendCollection(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = new ServiceCollection();

            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();


            // 1. Регистрируем сам чистый класс инспектора, чтобы EventBus смог его сожрать через конструктор!
            services.AddSingleton<EventBusInspector>();

            // 2. Регистрируем шину (DI-контейнер сам закинет туда инспектор, созданный строкой выше)
            services.AddSingleton<IEventBus, EventBus>();

            // 3. Перенаправляем интерфейс IEventInspector на ТОТ ЖЕ САМЫЙ экземпляр инспектора
            services.AddSingleton<IEventInspector>(provider =>
                provider.GetRequiredService<EventBusInspector>());


            // Логгер-неубивашка для моментального бэкапа
            IServiceCollection serviceCollection = services.AddSingleton<AppFileogger>(provider => new AppFileogger("App_debug.txt"));

            services.AddSingleton<ILogQueueManager>(provider =>
            {
                var eventBus = provider.GetRequiredService<IEventBus>();

                // Передаем шину событий и задержку в 20 миллисекунд
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


            // Контекст БД принимает строки подключения извне
            // 🎯 ФИКС: Передаем шину событий третьим параметром в конструктор контейнера контекста!
            services.AddSingleton(provider =>
                new DbContextContainer(
                    postgresConn,
                    sqliteConn,
                    provider.GetRequiredService<IEventBus>(), // 🔥 ДОПИСАЛИ СЮДА ПАДИТЕЛЬ ДЛЯ АВТОБУСА
                    provider.GetRequiredService<ILogger>()));


            // 2. Регистрация репозиториев и бизнес-сервисов (Работают везде, даже в консоли)
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<FriendRepository>();
            services.AddSingleton<IMessageRepository, MessageRepository>();

            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IRegistrationService, RegistrationService>();
            services.AddSingleton<IAuthenticateService, AuthenticateService>();
            services.AddSingleton<IDeleteService, DeleteService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IFriendService, FriendService>();
            services.AddSingleton<SiberNetSyncCoordinator>();


            // 3. Регистрация Вьюмоделей слоя Презентации


            services.AddSingleton<StatusBarViewModel>();
            // Регистрируем как Singleton, раз окно у нас одно
            services.AddSingleton<SystemButtonsViewModel>();

            
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<TerminalLogsViewModel>(); // 🔥 ДОБАВИЛИ НАШУ КРОССПЛАТФОРМЕННУЮ ВЬЮМОДЕЛЬ
                                                            // Вьюмодель инспектора для вывода отчета на экран
            services.AddSingleton<InspectorViewModel>();
           

            services.AddSingleton<AdminViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<TitleBarViewModel>();
            services.AddSingleton<SeedUsersViewModel>(); services.AddSingleton<TableUsersViewModel>();

            // 🔥 СТАЛО: Регистрируем конкретных наследников-автоматов
            services.AddSingleton<AdminNavigationManager>();
            services.AddSingleton<UserNavigationManager>();

            // Кастомный мост для обратной совместимости, если где-то захардкожена базовая ссылка
            services.AddSingleton<NavigationStateManager>(provider =>
                provider.GetRequiredService<UserNavigationManager>());

 services.AddSingleton<UserViewModel>();
            // Изолированная левая панель контактов
            services.AddSingleton<ContactsViewModel>();

            services.AddSingleton<MainViewModel>();

            

            return services;
        }

        /// <summary>
        /// Универсальный метод сборки чистого бэкенда (например, для консольного клиента)
        /// </summary>
        public static IServiceProvider Build(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = CreateBackendCollection(mode, postgresConn, sqliteConn);
            _serviceProvider = services.BuildServiceProvider();
            return _serviceProvider;
        }

        // Локатор для дата-контекста окон
        public static TGet GetViewModel<TGet>() where TGet : class
        {
            return ServiceProvider.GetRequiredService<TGet>();
        }


        public static void SetProvider(IServiceProvider provider)
        {
            _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

    }



    // Вставляй в самый конец файла AppBootstrapper.cs за пределами основного класса
    public class GuidTypeHandler : Dapper.SqlMapper.TypeHandler<Guid>
    {
        // Как записывать Guid в базу данных
        public override void SetValue(System.Data.IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        // Как без ошибок читать Guid из базы данных обратно в C#
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
