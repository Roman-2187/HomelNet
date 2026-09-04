using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeSocialNetwork.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet;
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace HomeSocialNetwork
{
    public partial class App : Application
    {
        #region Поля и свойства 
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _connectionString = $"Data Source={dbPath}";
        private MainWindow? _mainWindow;
        private IServiceProvider? _serviceProvider;

        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        public EventBus EventBus => _serviceProvider?.GetRequiredService<EventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");
        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                Debug.WriteLine("DI-контейнер успешно создан");

                // 1. Сначала обязательно будим логгер, чтобы окно логов открылось!
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();

                // 2. Инициализируем ядро базы данных
                var dbCore = _serviceProvider.GetRequiredService<DbInfrastructureCore>();
                dbCore.InitializeAsync(DatabaseType.SQLite).GetAwaiter().GetResult();

                // 3. 🔥 ВОЗВРАЩАЕМ ПИНОК ДЛЯ ТАБЛИЦЫ И ОКОН! 
                // Теперь они синглтоны, поэтому вызов GetRequiredService просто ОДИН раз создаст их в памяти.
                // Это запустит их внутренние подписки и стартовые потоки загрузки.
                _serviceProvider.GetRequiredService<UsersTableViewModel>();
                _serviceProvider.GetRequiredService<UsersTableViewModel>();
                _serviceProvider.GetRequiredService<UserDashboardViewModel>(); // 🔥 ПИНАЕМ ЗДЕСЬ!
                _serviceProvider.GetRequiredService<LogViewModel>();

                _serviceProvider.GetRequiredService<LogViewModel>();
                _serviceProvider.GetRequiredService<AdminMenuViewModel>();
                _serviceProvider.GetRequiredService<RegistrationViewModel>();
                _serviceProvider.GetRequiredService<AuthenticationViewModel>();

                // 4. Показываем главное окно
                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                _mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка запуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }


        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<EventBus>();
            services.AddSingleton<StatusBarViewModel>();

            // 🔥 ИСПРАВЛЕНО: Перевели UsersTableViewModel строго в Singleton!
            services.AddSingleton(provider =>
            {
                var core = provider.GetRequiredService<DbInfrastructureCore>();
                return new UsersTableViewModel(
                    provider.GetRequiredService<EventBus>(),
                    core.ListUsersService
                );
            });

            // Лог-менеджер
            services.AddSingleton(provider => new LogWindow(provider.GetRequiredService<ILogger>()));
            services.AddSingleton(provider =>
            {
                var logWin = provider.GetRequiredService<LogWindow>();
                var manager = new LogQueueManager(logWin, 5);
                provider.GetRequiredService<ILogger>().SetOutput(manager.WriteLog);
                return manager;
            });

            // 2. Регистрируем Ядро СУБД (Singleton)
            services.AddSingleton(provider =>
                new DbInfrastructureCore(_connectionString, provider.GetRequiredService<ILogger>()));

            // 3. Регистрация Вьюмоделей (Все Singleton — никаких дублей!)
            services.AddSingleton(provider =>
                new RegistrationViewModel(provider.GetRequiredService<DbInfrastructureCore>().RegisterService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton(provider =>
               new AuthenticationViewModel(
                    provider.GetRequiredService<DbInfrastructureCore>().AuthenticateService,
                    provider.GetRequiredService<EventBus>()));

            services.AddSingleton<LogViewModel>();

            services.AddSingleton(provider =>
                new AdminMenuViewModel(provider.GetRequiredService<DbInfrastructureCore>().UserService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton(provider =>
            {
                var core = provider.GetRequiredService<DbInfrastructureCore>();
                return new DeleteUsersViewModel(
                    core.DeleteService,
                    provider.GetRequiredService<EventBus>(),
                    provider.GetRequiredService<ILogger>()
                );
            });

            // 🔥 ИСПРАВЛЕНО: Чистое создание MainViewModel без "зависших" в воздухе вызовов
            services.AddSingleton(provider =>
            {
                return new MainViewModel(
                    provider.GetRequiredService<ILogger>(),
                    provider.GetRequiredService<EventBus>());
            });

            services.AddTransient<MainWindow>();


            services.AddSingleton(provider =>
        new UserDashboardViewModel(provider.GetRequiredService<EventBus>()));

            services.AddTransient<MainWindow>();
        }
    }
}
