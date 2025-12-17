using HomeNetCore.Data;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet;
using WpfHomeNet.Controls;
using WpfHomeNet.Interfaces;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;


namespace HomeSocialNetwork 
{

    public partial class App : Application
    {
        #region Поля и переменные 
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _connectionString = $"Data Source={dbPath}";
        private UserRepository? _userRepository;              
        private DbConnection? _connection;
        private DBInitializer? _databaseInitializer;
        private ISchemaProvider? _schemaProvider;
        private ISchemaSqlInitializer? _schemaSqlInit;
        private TableSchema? _tableSchema;
        private ISchemaUserSqlGenerator? _userSqlGen;
        private ISchemaAdapter? _schemaAdapter;
        private MainWindow? _mainWindow;

        public LogWindow LogWindow => _logWindow ?? throw new InvalidOperationException($"{nameof(_logWindow)} не инициализирован");
        public LogWindow? _logWindow;

        public UserService UserService => _userService ?? throw new InvalidOperationException($"{nameof(_userService)} не инициализирован");
        private UserService? _userService;

        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException($"{nameof(_mainVm)} не инициализирован");
        private MainViewModel? _mainVm;

        public ILogger Logger => _logger ?? throw new InvalidOperationException($"{nameof(_logger)} не инициализирован");
        private ILogger? _logger;

        public IStatusUpdater Status => _status ?? throw new InvalidOperationException($"{nameof(_status)} не инициализирован");
        private IStatusUpdater? _status;

        private LogQueueManager LogQueueManager => _logQueueManager ?? throw new InvalidOperationException($"{nameof(_logQueueManager)} не инициализирован");
        private LogQueueManager? _logQueueManager;

        private RegistrationViewModel? _registrationViewModel;
        public RegistrationViewModel RegistrationViewModel =>_registrationViewModel ?? throw new InvalidOperationException($"{nameof(_registrationViewModel)} не инициализирован");

        private LoginViewModel? _loginViewModel;
        public LoginViewModel LoginViewModel => _loginViewModel ?? throw new InvalidOperationException($"{nameof(_loginViewModel)} не инициализирован");
       
        LogViewModel? _logViewModel;
        LogViewModel LogViewModel =>_logViewModel ?? throw new InvalidOperationException($"{nameof(_logViewModel)} не инициализирован");

        private AdminMenuViewModel? _adminMenuViewModel;
        public AdminMenuViewModel AdminMenuViewModel => _adminMenuViewModel ?? throw new InvalidOperationException($"{nameof(_adminMenuViewModel)} не инициализирован");
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

                // Ключевая инициализация
                InitializeApplication(provider, DatabaseType.SQLite).GetAwaiter().GetResult();

                 _mainWindow = provider.GetRequiredService<MainWindow>();

                LogViewModel.ConnectToMainViewModel(MainVm);

                AdminMenuViewModel.ConnectToMainViewModel(MainVm);

                _mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                $"Ошибка запуска: {ex.Message}",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

                Shutdown();
            }
        }

        private async Task InitializeApplication(IServiceProvider provider, DatabaseType databaseType)
        {
            try
            {
                _logger = provider.GetRequiredService<ILogger>();
               

                _logWindow = new LogWindow(_logger);

               

                _logQueueManager = new LogQueueManager(LogWindow, 20);
                
                Logger.SetOutput(_logQueueManager.WriteLog);

          
                _logger.LogInformation("Начало инициализации приложения...");

                // 2. Создаём схему
                _tableSchema = new UsersTable().Build();

                // 3. Создаём factory
                var factory = new DatabaseServiceFactory(_connectionString, _logger);

                // 4. Получаем сервисы БД
                var (connection, sqlInit, schemaProvider, schemaAdapter, userSqlGen) =
                    factory.CreateServices(databaseType, _tableSchema);

                _connection = connection;
                _schemaSqlInit = sqlInit;
                _schemaProvider = schemaProvider;
                _schemaAdapter = schemaAdapter;
                _userSqlGen = userSqlGen;

                // 5. Инициализируем БД
                _databaseInitializer = new DBInitializer(
                    _connection, _schemaProvider, _schemaAdapter,
                    _schemaSqlInit, _tableSchema, _logger);
                await _databaseInitializer.InitializeAsync();

                _logger.LogInformation("БД инициализирована");

                // 6. Создаём репозиторий и сервис
                _userRepository = new UserRepository(_connection, _userSqlGen);
                _userService = new UserService(_userRepository, _logger);

                // 7. Создаём VM
                _registrationViewModel = new RegistrationViewModel(_userService);


                _loginViewModel = new LoginViewModel(_userService);

                _logViewModel = new LogViewModel();

                _adminMenuViewModel  = new AdminMenuViewModel(LogQueueManager);

                _mainVm = new MainViewModel(UserService, Logger, RegistrationViewModel, LoginViewModel,AdminMenuViewModel,LogWindow,_logViewModel);

                _logger.LogInformation("Инициализация завершена");
                
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Инициализация завершилась с ошибкой: {ex.Message}");
                Debug.WriteLine($"Ошибка: {ex.Message} | StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, Logger>();           
            services.AddSingleton<LogQueueManager>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegistrationViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<RegistrationViewControl>();
            services.AddTransient<LoginViewControl>();
            services.AddTransient<LoginViewModel>();
        }
    }



}

