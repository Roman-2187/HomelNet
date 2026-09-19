using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.OutputLogging.HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Builders;
using HomeNetOrm.Repositories;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Diagnostics;
using HomeNetServices.Identity;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HomeNet.DI
{
    public static class AppBootstrapper
    {
        private static IServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider => _serviceProvider
            ?? throw new InvalidOperationException("Контейнер еще не собран. Вызовите Build().");

        // 🔥 УНИВЕРСАЛЬНЫЙ МЕТОД СБОРКИ КАБЕЛЯ (Для консоли, WPF, Avalonia)
        public static IServiceProvider Build(BackendMode mode, string postgresConn, string sqliteConn)
        {
            var services = new ServiceCollection();

            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<StatusBarViewModel>();

            // 🔥 Логгер-менеджер для админки
            services.AddSingleton<ILogQueueManager>(provider =>
            {
                var uiManager = new LogQueueManager(0);
                var crashLogger = provider.GetRequiredService<AppCrashLogger>();

                // 🔥 ВОТ ОНО! Добавляем 'ns' третьим параметром в экшен!
                provider.GetRequiredService<ILogger>().SetOutput((msg, level, ns) =>
                {
                    // 1. Текстовик мгновенно бэкапит строку на диск
                    crashLogger.WriteImmediately(msg, level);

                    // 2. Менеджер очереди забирает строку И неймспейс на посимвольную анимацию в UI
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
            services.AddSingleton<UsersTableViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<AdminMenuViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<ChatViewModel>();

            // 🔥 ДОБАВИЛИ: Изолированная левая панель контактов
            services.AddSingleton<ContactsListViewModel>();

            // 🔥 Контейнер дашборда автоматически подтянет ContactsListViewModel и ChatViewModel
            services.AddSingleton<UserDashboardViewModel>();

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
