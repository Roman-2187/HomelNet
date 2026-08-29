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
using HomeNetCore.Services.DeleteService; 
using HomeNetCore.Services.ListUsersServise;
using HomeSocialNetwork.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet;
using WpfHomeNet.Controls;
using WpfHomeNet.Interfaces;
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace HomeSocialNetwork
{
    public partial class App : Application
    {
        #region Поля и переменные 

        //отсюда 
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
        private ListUsersService? _listUsersService;
       
        private UserService? _userService; 
        private ILogger? _logger;

        // и до сюда хочу попробовать пихнуть в ядро правда как ним потом обращатся тоже через подписки 
        private MainWindow? _mainWindow;
        private MainViewModel? _mainVm;
        private LogWindow? _logWindow;
        private LogQueueManager? _logQueueManager;
        private IStatusUpdater? _status = null;
        private AuthenticationViewModel? _loginViewModel;
        
        private RegistrationViewModel? _registrationViewModel;


        public ListUsersService ListUsersService => _listUsersService ?? throw new InvalidOperationException($"{nameof(_listUsersService)} не инициализирован");
       

        public DeletionUsersViewModel DeleteUsersModel => _deleteUsersModel ?? throw new InvalidOperationException($"{nameof(_deleteUsersModel)} не инициализирован");
        private DeletionUsersViewModel? _deleteUsersModel;

        public LogWindow LogWindow => _logWindow ?? throw new InvalidOperationException($"{nameof(_logWindow)} не инициализирован");
        

        public UserService UserService => _userService ?? throw new InvalidOperationException($"{nameof(_userService)} не инициализирован");
        

        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException($"{nameof(_mainVm)} не инициализирован");
        

        public ILogger Logger => _logger ?? throw new InvalidOperationException($"{nameof(_logger)} не инициализирован");
        

        public IStatusUpdater Status => _status ?? throw new InvalidOperationException($"{nameof(_status)} не инициализирован");
        
        private LogQueueManager LogQueueManager => _logQueueManager ?? throw new InvalidOperationException($"{nameof(_logQueueManager)} не инициализирован");
        

        public RegistrationViewModel RegistrationViewModel => _registrationViewModel ?? throw new InvalidOperationException($"{nameof(_registrationViewModel)} не инициализирован");
        

        public AuthenticationViewModel LoginViewModel => _loginViewModel ?? throw new InvalidOperationException($"{nameof(_loginViewModel)} не инициализирован");
        

        private LogViewModel LogViewModel => _logViewModel ?? throw new InvalidOperationException($"{nameof(_logViewModel)} не инициализирован");
        private LogViewModel? _logViewModel;

        public AdminMenuViewModel AdminMenuViewModel => _adminMenuViewModel ?? throw new InvalidOperationException($"{nameof(_adminMenuViewModel)} не инициализирован");
        private AdminMenuViewModel? _adminMenuViewModel;

        private EventBus? _eventBus;
        public EventBus EventBus => _eventBus ?? throw new InvalidOperationException($"{nameof(_eventBus)} не инициализирован");
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
                _eventBus = provider.GetRequiredService<EventBus>(); // Забираем шину из DI

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

                // Создаем сервисы ядра в нужном порядке
                var registerService = new RegisterService(_userService);
                var authenticateService = new AuthenticateService(_userService);
                var deleteService = new DeleteService(_userService); // Сервис ядра для удаления

                // Инициализируем вьюмодели с правильными абстракциями и шиной сообщений
                _registrationViewModel = new RegistrationViewModel(registerService,EventBus);
                _loginViewModel = new AuthenticationViewModel(authenticateService);

                // ПРАВКА: Передаем в LogViewModel шину, менеджер и само созданное окно логов
                _logViewModel = new LogViewModel(EventBus, LogQueueManager, LogWindow);

                // ПРАВКА: Передаем ListUsersService в админку для обновления списков
                _adminMenuViewModel = new AdminMenuViewModel(UserService,EventBus);

                // ПРАВКА: Передаем правильный DeleteService в вьюмодель удаления
                _deleteUsersModel = new DeletionUsersViewModel(deleteService,EventBus);

               

                _mainVm = new MainViewModel(Logger, EventBus, ListUsersService)
                {
                    // ПРАВКА: Принудительно связываем дочерние формы с главной моделью!
                    RegistrationViewModel = _registrationViewModel,
                    LoginViewModel = _loginViewModel,
                    LogVm = _logViewModel,
                    AdminMenuViewModel = _adminMenuViewModel,
                    DeleteUsersViewModel = _deleteUsersModel,
                    LogWindow = _logWindow
                };

                _logger.LogInformation("Инициализация завершена");


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
            services.AddSingleton<EventBus>(); // Регистрируем наш глобальный автобус сообщений

            // Передаем фабричные методы, чтобы контейнер не ругался на nullable-свойства во время компиляции
            IServiceCollection serviceCollection = services.AddSingleton<RegistrationViewModel>(_ => RegistrationViewModel);
            services.AddSingleton<AuthenticationViewModel>(_ => LoginViewModel);
            services.AddSingleton<MainViewModel>(_ => MainVm);

            services.AddTransient<MainWindow>();
            services.AddTransient<RegistrationViewControl>();
            services.AddTransient<LoginViewControl>();
        }
    }


    
// ... остальные юзинги остаются прежними


        //public partial class App : Application
        //{
        //    #region Поля и свойства — Стерильная чистота!
        //    private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        //    private readonly string _connectionString = $"Data Source={dbPath}";

        //    // Всего одна готовая деталь инфраструктуры вместо 12 пузатых полей!
        //    private DbInfrastructureCore? _dbCore;
        //    private MainWindow? _mainWindow;

        //    // Системные синглтоны
        //    private ILogger? _logger;
        //    private EventBus? _eventBus;
        //    private LogQueueManager? _logQueueManager;
        //    private LogWindow? _logWindow;

        //    // Вьюмодели
        //    private MainViewModel? _mainVm;
        //    private RegistrationViewModel? _registrationViewModel;
        //    private AuthenticationViewModel? _loginViewModel;
        //    private LogViewModel? _logViewModel;
        //    private AdminMenuViewModel? _adminMenuViewModel;
        //    private DeletionUsersViewModel? _deleteUsersModel;
        //    #endregion

        //    // ... метод OnStartup остаётся без изменений ...

        //    private async Task InitializeApplication(IServiceProvider provider, DatabaseType databaseType)
        //    {
        //        try
        //        {
        //            _logger = provider.GetRequiredService<ILogger>();
        //            _eventBus = provider.GetRequiredService<EventBus>();

        //            _logWindow = new LogWindow(_logger);
        //            _logQueueManager = new LogQueueManager(_logWindow, 20);
        //            _logger.SetOutput(_logQueueManager.WriteLog);
        //            _logger.LogInformation("Инициализация приложения...");

        //            // 1. СОБИРАЕМ ДЕТАЛЬ: Инициализируем наше инфраструктурное ядро одной строчкой!
        //            _dbCore = new DbInfrastructureCore(_connectionString, _logger);
        //            await _dbCore.InitializeAsync(databaseType);

        //            // 2. ИСПОЛЬЗУЕМ ЧЕРЕЗ ТОЧКУ: Контейнер DI и конструкторы забирают всё в чистом виде!
        //            _registrationViewModel = new RegistrationViewModel(_dbCore.RegisterService, _eventBus);
        //            _loginViewModel = new AuthenticationViewModel(_dbCore.AuthenticateService);
        //            _logViewModel = new LogViewModel(_eventBus, _logQueueManager, _logWindow);
        //            _adminMenuViewModel = new AdminMenuViewModel(_dbCore.UserService, _eventBus);
        //            _deleteUsersModel = new DeletionUsersViewModel(_dbCore.DeleteService, _eventBus);

        //            _mainVm = new MainViewModel(_logger, _eventBus, _dbCore.ListUsersService)
        //            {
        //                RegistrationViewModel = _registrationViewModel,
        //                LoginViewModel = _loginViewModel,
        //                LogVm = _logViewModel,
        //                AdminMenuViewModel = _adminMenuViewModel,
        //                DeleteUsersViewModel = _deleteUsersModel,
        //                LogWindow = _logWindow
        //            };

        //            _logger.LogInformation("Инициализация успешно завершена. Код чист!");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger?.LogError($"Критическая ошибка инициализации: {ex.Message}");
        //            throw;
        //        }
        //    }

        //    // ... метод ConfigureServices остаётся без изменений ...
        //}
    }



