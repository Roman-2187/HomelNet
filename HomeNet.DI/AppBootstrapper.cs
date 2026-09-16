using Microsoft.Extensions.DependencyInjection;
using HomeNetCore.Interfaces;
using HomeNetOrm.Repositories;
using HomeNetServices.Services.Identity;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Diagnostics;
using HomeNetServices.Routing;
using HomeNetOrm.Builders;
using HomeNetCore.Enums;
using HomeNetOrm.Interfaces;
using HomeNetServices.Identity; // или using HomeNetCore;


namespace HomeNet.DI
{
    public static class AppBootstrapper
    {
        private static IServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider => _serviceProvider
            ?? throw new InvalidOperationException("Контейнер еще не собран. Вызовите Build().");

        // 🔥 УНИВЕРСАЛЬНЫЙ МЕТОД СБОРКИ КАБЕЛЯ
        public static IServiceProvider Build(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = new ServiceCollection();

            // 1. Системная инфраструктура (Singleton) — одинаковая для всех!
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<StatusBarViewModel>();

            services.AddSingleton<LogQueueManager>(provider =>
            {
                var manager = new LogQueueManager(0);
                provider.GetRequiredService<ILogger>().SetOutput((msg, level) => manager.WriteLog(msg, level));
                return manager;
            });

            // Контекст БД принимает строки подключения извне
            services.AddSingleton(provider =>
                new DbContextContainer(postgresConn, sqliteConn, provider.GetRequiredService<ILogger>()));

            // 2. Регистрация репозиториев и бизнес-сервисов
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<FriendRepository>();
            services.AddSingleton<IMessageRepository, MessageRepository>();

            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IRegisterService, RegisterService>();
            services.AddSingleton<IAuthenticateService, AuthenticateService>();
            services.AddSingleton<IDeleteService, DeleteService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IFriendService, FriendService>();

            // 3. Регистрация Вьюмоделей слоя Презентации (Они доступны и для консоли, и для WPF!)
            services.AddSingleton<UsersTableViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<AdminMenuViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<UserDashboardViewModel>();
            services.AddSingleton<ChatViewModel>();
            services.AddTransient<MainViewModel>();

            _serviceProvider = services.BuildServiceProvider();
            return _serviceProvider;
        }

        // 🔥 ТОТ САМЫЙ ЛОКАТОР НА БЛЮДЕЧКЕ ДЛЯ ДАТА-КОНТЕКСТА
        public static TGet GetViewModel<TGet>() where TGet : class
        {
            return ServiceProvider.GetRequiredService<TGet>();
        }
    }
}
