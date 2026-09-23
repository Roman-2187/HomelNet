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
using HomeNetServices.Diagnostics;
using HomeNetServices.Identity;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
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
            services.AddSingleton<IEventBus, EventBus>();

            // Логгер-неубивашка для моментального бэкапа
            IServiceCollection serviceCollection = services.AddSingleton<AppFileogger>(provider => new AppFileogger("App_debug.txt"));

            services.AddSingleton<ILogQueueManager>(provider =>
            {
                var eventBus = provider.GetRequiredService<IEventBus>();

                // Передаем шину событий и задержку в 20 миллисекунд
                var uiManager = new HomeNetServices.Diagnostics.LogQueueManager(eventBus, 15);

                var crashLogger = provider.GetRequiredService<AppFileogger>();

                provider.GetRequiredService<ILogger>().SetOutput((msg, level, ns) =>
                {
                    crashLogger.WriteImmediately(msg, level);
                    uiManager.WriteLog(msg, level, ns);
                });

                return uiManager;
            });




            // Контекст БД принимает строки подключения извне
            services.AddSingleton(provider =>
                new DbContextContainer(postgresConn, sqliteConn, provider.GetRequiredService<ILogger>()));

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

            // 3. Регистрация Вьюмоделей слоя Презентации
             
            services.AddSingleton<StatusBarViewModel>();
            services.AddSingleton<UsersTableViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<TerminalViewModel>(); // 🔥 ДОБАВИЛИ НАШУ КРОССПЛАТФОРМЕННУЮ ВЬЮМОДЕЛЬ
            services.AddSingleton<AdminMenuViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<TitleBarViewModel>();
            
           

            // Регистрируем навигатор как Singleton, чтобы он жил в одном экземпляре на всё приложение
            services.AddSingleton<NavigationStateManager>();
            

            // Изолированная левая панель контактов
            services.AddSingleton<ContactsListViewModel>();

            // 🍔 Кастомное меню аккаунта (выпадашка "Профиль / Выход")
            // Делаем Transient, чтобы для админки и дашборда создавались свои независимые экземпляры плашки
            // Было: services.AddTransient<AccountMenuViewModel>();
            // Стало: Теперь это железобетонный синглтон!
            services.AddSingleton<AccountMenuViewModel>();


            // Контейнер дашборда автоматически подтянет ContactsListViewModel и ChatViewModel
            services.AddSingleton<UserDashboardViewModel>();

            services.AddTransient<MainViewModel>();

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
}
