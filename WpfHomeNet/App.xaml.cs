using HomeNetCore.Data;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using HomeNetCore.Services.AuthenticationService;
using HomeNetCore.Services.ListUsersServise;
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

        public ListUsersService ListUsersService => _listUsersService ?? throw new InvalidOperationException($"{nameof(_listUsersService)} не инициализирован");
        private ListUsersService? _listUsersService;

        public DeleteUsersViewModel DeleteUsersModel => _deleteUsersModel ?? throw new InvalidOperationException($"{nameof(_deleteUsersModel)} не инициализирован");
        private DeleteUsersViewModel? _deleteUsersModel;

        public LogWindow LogWindow => _logWindow ?? throw new InvalidOperationException($"{nameof(_logWindow)} не инициализирован");
        private LogWindow? _logWindow;

        public UserService UserService => _userService ?? throw new InvalidOperationException($"{nameof(_userService)} не инициализирован");
        private UserService? _userService;

        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException($"{nameof(_mainVm)} не инициализирован");
        private MainViewModel? _mainVm;

        public ILogger Logger => _logger ?? throw new InvalidOperationException($"{nameof(_logger)} не инициализирован");
        private ILogger? _logger;

        public IStatusUpdater Status => _status ?? throw new InvalidOperationException($"{nameof(_status)} не инициализирован");
        private IStatusUpdater? _status = null;

        private LogQueueManager LogQueueManager => _logQueueManager ?? throw new InvalidOperationException($"{nameof(_logQueueManager)} не инициализирован");
        private LogQueueManager? _logQueueManager;

        public RegistrationViewModel RegistrationViewModel => _registrationViewModel ?? throw new InvalidOperationException($"{nameof(_registrationViewModel)} не инициализирован");
        private RegistrationViewModel? _registrationViewModel;

        public LoginInViewModel LoginViewModel => _loginViewModel ?? throw new InvalidOperationException($"{nameof(_loginViewModel)} не инициализирован");
        private LoginInViewModel? _loginViewModel;

        private LogViewModel LogViewModel => _logViewModel ?? throw new InvalidOperationException($"{nameof(_logViewModel)} не инициализирован");
        private LogViewModel? _logViewModel;

        public AdminMenuViewModel AdminMenuViewModel => _adminMenuViewModel ?? throw new InvalidOperationException($"{nameof(_adminMenuViewModel)} не инициализирован");
        private AdminMenuViewModel? _adminMenuViewModel;
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

                LogViewModel.ConnectToMainViewModel(MainVm);
                AdminMenuViewModel.ConnectToMainViewModel(MainVm);
                RegistrationViewModel.ConnectToMainViewModel(MainVm);

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

                _tableSchema = new UsersTable().Build();
                var factory = new DatabaseServiceFactory(_connectionString, _logger);

                var (connection, sqlInit, schemaProvider, schemaAdapter, userSqlGen) =
                    factory.CreateServices(databaseType, _tableSchema);

                _connection = connection;
                _schemaSqlInit = sqlInit;
                _schemaProvider = schemaProvider;
                _schemaAdapter = schemaAdapter;
                _userSqlGen = userSqlGen;

                _databaseInitializer = new DBInitializer(
                    _connection, _schemaProvider, _schemaAdapter,
                    _schemaSqlInit, _tableSchema, _logger);
                await _databaseInitializer.InitializeAsync();

                _logger.LogInformation("БД инициализирована");

                _userRepository = new UserRepository(_connection, _userSqlGen);
                _userService = new UserService(_userRepository, _logger);
                _listUsersService = new ListUsersService(_userService);

                // Создаем наши новые сервисы ядра строго в нужном порядке
                var registerService = new RegisterService(_userService);
                var authenticateService = new AuthenticateService(_userService);

                // Инициализируем вьюмодели
                _registrationViewModel = new RegistrationViewModel(registerService);
                _loginViewModel = new LoginInViewModel(authenticateService);
                _logViewModel = new LogViewModel(LogQueueManager);
                _adminMenuViewModel = new AdminMenuViewModel(_userService);
                _deleteUsersModel = new DeleteUsersViewModel(_userService);

                _mainVm = new MainViewModel(
                    Logger, RegistrationViewModel,
                    LoginViewModel, AdminMenuViewModel, LogWindow, LogViewModel, DeleteUsersModel, ListUsersService);

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

            // Передаем фабричные методы, чтобы контейнер не ругался на nullable-свойства во время компиляции
            services.AddSingleton<RegistrationViewModel>(_ => RegistrationViewModel);
            services.AddSingleton<LoginInViewModel>(_ => LoginViewModel);
            services.AddSingleton<MainViewModel>(_ => MainVm);

            services.AddTransient<MainWindow>();
            services.AddTransient<RegistrationViewControl>();
            services.AddTransient<LoginViewControl>();
        }
    }
}

