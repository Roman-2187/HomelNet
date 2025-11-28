using HomeNetCore.Data;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Enums;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    
    public partial class MainWindow : Window
    {
        #region Поля и переменные
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _connectionString = $"Data Source={dbPath}";
        public LogWindow LogWindow => _logWindow ?? throw new InvalidOperationException("_logWindow не инициализирован");
        public LogWindow? _logWindow;
        private UserService UserService => _userService  ?? throw new InvalidOperationException("_userService не инициализирован");
        private UserService? _userService;
        private MainViewModel MainVm => _mainVm?? throw new InvalidOperationException("_mainVm не инициализирован");      
        private MainViewModel? _mainVm;
        private ILogger Logger => _logger ?? throw new InvalidOperationException("_logger не инициализирован"); 
        private ILogger? _logger;
        private IStatusUpdater Status =>  _status ?? throw new InvalidOperationException("_status не инициализирован");
        private IStatusUpdater? _status; 
        private DbConnection? _connection;
        private DBInitializer? _databaseInitializer;
        private ISchemaProvider? _schemaProvider;
        private ISchemaSqlInitializer? _schemaSqlInit;
        private TableSchema? _tableSchema;
        private ISchemaUserSqlGenerator? _userSqlGen;
        private ISchemaAdapter? _schemaAdapter;
        private LogQueueManager? _logQueueManager;
        private UserRepository? _userRepository;
        #endregion


        public MainWindow()
        {
            InitializeComponent();

            InitializeLogging();

            CenterMainAndHideLogs();
        }


        private void InitializeLogging()
        {
            _logger = new Logger();
            _logWindow = new LogWindow(Logger);
            _logQueueManager = new LogQueueManager(LogWindow,1);
            Logger.SetOutput(_logQueueManager.WriteLog);

            Logger.LogInformation($"Путь БД: {dbPath}");
            Logger.LogInformation("Application started. PID: " + Process.GetCurrentProcess().Id);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitializeAsync(DatabaseType.SQLite);

                await PostInitializeAsync();

                await LoadUsersOnStartupAsync();
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Критическая ошибка при запуске: {ex.Message}");
                MessageBox.Show(
                    $"Произошла ошибка при запуске приложения: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }


        private async Task InitializeAsync(DatabaseType databaseType)
        {
            try
            {
                _tableSchema = new UsersTable().Build();
              
                var factory = new DatabaseServiceFactory(_connectionString, Logger);

                // 2. Получаем все сервисы одним вызовом
                var (connection, sqlInit, schemaProvider, schemaAdapter, userSqlGen) =
                    factory.CreateServices(databaseType, _tableSchema);

                // 3. Сохраняем в поля класса 
                _connection = connection;
                _schemaSqlInit = sqlInit;
                _schemaProvider = schemaProvider;
                _schemaAdapter = schemaAdapter;
                _userSqlGen = userSqlGen;

                _databaseInitializer = new DBInitializer(
                    _connection, _schemaProvider,
                    _schemaAdapter, _schemaSqlInit,
                    _tableSchema, Logger);

                // Асинхронное ожидание инициализации БД
                await _databaseInitializer.InitializeAsync();

                _userRepository = new UserRepository(_connection, _userSqlGen);

               _userService = new UserService(_userRepository, Logger);

                // Создание ViewModel
                _mainVm = new MainViewModel(UserService, Logger);

            }
            catch (Exception ex)
            {
                Logger?.LogError($"Инициализация завершилась с ошибкой: {ex.Message}");

                MessageBox.Show(
                    $"Произошла критическая ошибка при инициализации: {ex.Message}",
                    "Ошибка инициализации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        public async Task PostInitializeAsync()
        {
            // Проверка критических зависимостей
            if (Logger is null)
            {
                throw new InvalidOperationException("_logger не инициализирован");
            }

            if (_logQueueManager is null)
            {
                throw new InvalidOperationException("_logQueueManager не инициализирован");
            }

            _status = (IStatusUpdater)MainVm;

            DataContext = Status;
        }

        private async Task LoadUsersOnStartupAsync()
        {
            try
            {                
                    await MainVm.LoadUsersAsync();                                           
            }
           
            catch (Exception ex)
            {
                Logger?.LogError("Ошибка загрузки пользователей при старте: " + ex.Message);
                MessageBox.Show(
                    $"Не удалось загрузить пользователей: {ex.Message}",
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ShowWindowLogs(LogWindow logWindow)
        {            
            this.Left = 20;
            logWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            logWindow.Left = this.Left + 1005;
            logWindow.Top = this.Top;
            logWindow.Show();

            btnLogs.Content = "Скрыть логи";
        }

        private void CenterMainAndHideLogs()
        {            
            this.Left = 150;
            this.Top = 200;
            btnLogs.Content = "Показать логи";
        }   
    }
}