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

        // Оставляем геттер автобуса для совместимости, вытаскивая его из живого провайдера
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
                      
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();
           
                var dbCore = _serviceProvider.GetRequiredService<DbInfrastructureCore>();
                dbCore.InitializeAsync(DatabaseType.SQLite).GetAwaiter().GetResult();

                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                _mainWindow.Show();


                _serviceProvider.GetRequiredService<RegistrationViewModel>();
                _serviceProvider.GetRequiredService<AuthenticationViewModel>();
                _serviceProvider.GetRequiredService<DeleteUsersViewModel>();
                _serviceProvider.GetRequiredService<AdminMenuViewModel>();
                _serviceProvider.GetRequiredService<LogViewModel>();

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
            services.AddSingleton<EventBus>(); // Наше любимое «Бюро вакансий»
            services.AddSingleton<StatusBarViewModel>();

            services.AddTransient<UsersTableViewModel>(provider =>
            {
                // Достаем наше ядро базы данных
                var core = provider.GetRequiredService<DbInfrastructureCore>();

                // Передаем в конструктор шину и сервис из ядра
                return new UsersTableViewModel(
                    provider.GetRequiredService<EventBus>(),
                    core.ListUsersService 
                );
            });

            // Лог-менеджер настраиваем через фабрику контейнера
            services.AddSingleton<LogWindow>(provider => new LogWindow(provider.GetRequiredService<ILogger>()));
            services.AddSingleton<LogQueueManager>(provider =>
            {
                var logWin = provider.GetRequiredService<LogWindow>();
                var manager = new LogQueueManager(logWin, 20);
                provider.GetRequiredService<ILogger>().SetOutput(manager.WriteLog);
                return manager;
            });


            // 2. Регистрируем готовую деталь Ядра СУБД
            services.AddSingleton<DbInfrastructureCore>(provider =>
                new DbInfrastructureCore(_connectionString, provider.GetRequiredService<ILogger>()));

            // 3. Автоматическая регистрация Вьюмоделей!
            // Контейнер сам залезет в их конструкторы, вытащит из DbInfrastructureCore нужные сервисы и подставит!
            services.AddSingleton<RegistrationViewModel>(provider =>
                new RegistrationViewModel(provider.GetRequiredService<DbInfrastructureCore>().RegisterService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton<AuthenticationViewModel>(provider =>
               new AuthenticationViewModel(
             provider.GetRequiredService<DbInfrastructureCore>().AuthenticateService,
             provider.GetRequiredService<EventBus>()));

            services.AddSingleton<LogViewModel>();

            services.AddSingleton<AdminMenuViewModel>(provider =>
                new AdminMenuViewModel(provider.GetRequiredService<DbInfrastructureCore>().UserService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton<DeleteUsersViewModel>(provider =>
            {
                var core = provider.GetRequiredService<DbInfrastructureCore>();

                return new DeleteUsersViewModel(
                    core.DeleteService,
                    provider.GetRequiredService<EventBus>(),
                    provider.GetRequiredService<ILogger>() // 🧼 ВРЕЗАЛИ: достаём логгер из контейнера!
                );
            });



            // Внутри App.xaml.cs возвращаем фабрику к стерильному виду:
            services.AddSingleton<MainViewModel>(provider =>
            {
                var core = provider.GetRequiredService<DbInfrastructureCore>();

                // Вытаскиваем только что зарегистрированную таблицу из контейнера
                var usersTableVm = provider.GetRequiredService<UsersTableViewModel>();

                var mainVm = new MainViewModel(
                    provider.GetRequiredService<ILogger>(),
                    provider.GetRequiredService<EventBus>());
                   
                return mainVm;
            });

            services.AddTransient<MainWindow>();
        }
    }
}
