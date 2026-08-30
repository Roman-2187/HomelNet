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
        #region Поля и переменные — Идеальная чистота!
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _connectionString = $"Data Source={dbPath}";

        private DbInfrastructureCore? _dbCore;
        private MainWindow? _mainWindow;
        private MainViewModel? _mainVm;
        private LogWindow? _logWindow;
        private LogQueueManager? _logQueueManager;
        private AuthenticationViewModel? _loginViewModel;
        private RegistrationViewModel? _registrationViewModel;
        private DeletionUsersViewModel? _deleteUsersModel;
        private LogViewModel? _logViewModel;
        private AdminMenuViewModel? _adminMenuViewModel;
        private ILogger? _logger;
        private EventBus? _eventBus;

        // Понятные геттеры для DI-контейнера
        public EventBus EventBus => _eventBus ?? throw new InvalidOperationException("EventBus не инициализирован");
        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException("MainVm не инициализирован");
        public RegistrationViewModel RegistrationViewModel => _registrationViewModel ?? throw new InvalidOperationException("RegistrationViewModel не инициализирован");
        public AuthenticationViewModel LoginViewModel => _loginViewModel ?? throw new InvalidOperationException("LoginViewModel не инициализирован");
        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                var provider = services.BuildServiceProvider();

                Debug.WriteLine("DI-контейнер создан");

                InitializeApplication(provider, DatabaseType.SQLite).GetAwaiter().GetResult();

                _mainWindow = provider.GetRequiredService<MainWindow>();
                _mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async Task InitializeApplication(IServiceProvider provider, DatabaseType databaseType)
        {
            try
            {
                _logger = provider.GetRequiredService<ILogger>();
                _eventBus = provider.GetRequiredService<EventBus>();

                _logWindow = new LogWindow(_logger);
                _logQueueManager = new LogQueueManager(_logWindow, 20);
                _logger.SetOutput(_logQueueManager.WriteLog);
                _logger.LogInformation("Начало инициализации приложения...");

                // 1. СОБИРАЕМ ДЕТАЛЬ: Одна строчка кода убирает всю низкоуровневую грязь
                _dbCore = new DbInfrastructureCore(_connectionString, _logger);
                await _dbCore.InitializeAsync(databaseType);

                _logger.LogInformation("БД инициализирована через ядро");

                // 2. ОБРАЩАЕМСЯ ЧЕРЕЗ ТОЧКУ: Красиво, строго и типизировано
                _registrationViewModel = new RegistrationViewModel(_dbCore.RegisterService, EventBus);
                _loginViewModel = new AuthenticationViewModel(_dbCore.AuthenticateService);
                _logViewModel = new LogViewModel(EventBus, _logQueueManager, _logWindow);
                _adminMenuViewModel = new AdminMenuViewModel(_dbCore.UserService, EventBus);
                _deleteUsersModel = new DeletionUsersViewModel(_dbCore.DeleteService, EventBus);

                _mainVm = new MainViewModel(_logger, EventBus, _dbCore.ListUsersService)
                {
                    RegistrationViewModel = _registrationViewModel,
                    LoginViewModel = _loginViewModel,
                    LogVm = _logViewModel,
                    AdminMenuViewModel = _adminMenuViewModel,
                    DeleteUsersViewModel = _deleteUsersModel,
                    LogWindow = _logWindow
                };

                _logger.LogInformation("Инициализация завершена. Код кристально чист!");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Инициализация завершилась с ошибкой: {ex.Message}");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<LogQueueManager>();
            services.AddSingleton<EventBus>();

            services.AddSingleton<RegistrationViewModel>(_ => RegistrationViewModel);
            services.AddSingleton<AuthenticationViewModel>(_ => LoginViewModel);
            services.AddSingleton<MainViewModel>(_ => MainVm);

            services.AddTransient<MainWindow>();
        }
    }
}



