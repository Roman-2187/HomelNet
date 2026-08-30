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

                
                // ИСПРАВЛЕНИЕ: Принудительно пинаем контейнер, чтобы он СРАЗУ создал лог-менеджер
                // и привязал SetOutput до того, как СУБД начнет писать свои логи!
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();
           
                var dbCore = _serviceProvider.GetRequiredService<DbInfrastructureCore>();
                dbCore.InitializeAsync(DatabaseType.SQLite).GetAwaiter().GetResult();

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
            services.AddSingleton<EventBus>(); // Наше любимое «Бюро вакансий»

            // Лог-менеджер настраиваем через фабрику контейнера
            services.AddSingleton<LogWindow>(provider => new LogWindow(provider.GetRequiredService<ILogger>()));
            services.AddSingleton<LogQueueManager>(provider =>
            {
                var logWin = provider.GetRequiredService<LogWindow>();
                var manager = new LogQueueManager(logWin, 20);
                provider.GetRequiredService<ILogger>().SetOutput(manager.WriteLog);
                return manager;
            });

            // ДОПИШИ ЭТУ СТРОКУ в верхнюю часть метода ConfigureServices:
            services.AddSingleton<StatusBarViewModel>();


            // 2. Регистрируем готовую деталь Ядра СУБД
            services.AddSingleton<DbInfrastructureCore>(provider =>
                new DbInfrastructureCore(_connectionString, provider.GetRequiredService<ILogger>()));

            // 3. Автоматическая регистрация Вьюмоделей!
            // Контейнер сам залезет в их конструкторы, вытащит из DbInfrastructureCore нужные сервисы и подставит!
            services.AddSingleton<RegistrationViewModel>(provider =>
                new RegistrationViewModel(provider.GetRequiredService<DbInfrastructureCore>().RegisterService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton<AuthenticationViewModel>(provider =>
                new AuthenticationViewModel(provider.GetRequiredService<DbInfrastructureCore>().AuthenticateService));

            services.AddSingleton<LogViewModel>();

            services.AddSingleton<AdminMenuViewModel>(provider =>
                new AdminMenuViewModel(provider.GetRequiredService<DbInfrastructureCore>().UserService, provider.GetRequiredService<EventBus>()));

            services.AddSingleton<DeletionUsersViewModel>(provider =>
                new DeletionUsersViewModel(provider.GetRequiredService<DbInfrastructureCore>().DeleteService, provider.GetRequiredService<EventBus>()));


            // Внутри App.xaml.cs возвращаем фабрику к стерильному виду:
            services.AddSingleton<MainViewModel>(provider =>
            {
                var core = provider.GetRequiredService<DbInfrastructureCore>();
                var mainVm = new MainViewModel(
                    provider.GetRequiredService<ILogger>(),
                    provider.GetRequiredService<EventBus>(),
                    core.ListUsersService);

                // Связываем свойства (без всяких bus.Subscribe!)
                mainVm.RegistrationViewModel = provider.GetRequiredService<RegistrationViewModel>();
                mainVm.LoginViewModel = provider.GetRequiredService<AuthenticationViewModel>();
                mainVm.LogVm = provider.GetRequiredService<LogViewModel>();
                mainVm.AdminMenuViewModel = provider.GetRequiredService<AdminMenuViewModel>();
                mainVm.DeleteUsersViewModel = provider.GetRequiredService<DeletionUsersViewModel>();
                mainVm.LogWindow = provider.GetRequiredService<LogWindow>();
                mainVm.StatusBarViewModel = provider.GetRequiredService<StatusBarViewModel>();

                return mainVm;
            });

            services.AddTransient<MainWindow>();
        }
    }
}
