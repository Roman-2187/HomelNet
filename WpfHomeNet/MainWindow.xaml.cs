using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.SqliteClasses;
using HomeNetCore.Data.TableSchemas;
using HomeNetCore.Helpers;
using HomeNetCore.Services;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet.Data.DBProviders.SqliteClasses;
using WpfHomeNet.Data.SqliteClasses;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
   
    public partial class MainWindow : Window
    {
        private readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private string _connection;
        private DBTableInitializer _databaseInitializer;
        private SqliteGetSchemaProvider _schemaProvider;
        private SqliteSchemaSqlInit _schemaSqlInit;
        private TableSchema _tableSchema;        
        private SqliteConnection _sqliteConnection;
        private LogManager _logManager;
        public  LogWindow _logWindow;
        private UserService _userService;
        private MainViewModel _mainVm; 
        private IStatusUpdater _status;
        private ILogger _logger;
        private SqliteUserSqlGen _userSqlGen;
        private SqliteSchemaAdapter _sqliteSchemaAdapter;

        public MainWindow()
        {
            InitializeComponent();
            this.Left = 20;

            // Запуск асинхронной инициализации с обработкой ошибок
            InitializeAsync().ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    MessageBox.Show(
                        $"Критическая ошибка при запуске: {task.Exception.InnerException?.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Close();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task InitializeAsync()
        {
            try
            {                
                 _connection = $"Data Source={dbPath}";
                 _sqliteConnection = new SqliteConnection(_connection);
                 _logWindow = new LogWindow();
                 _logManager = new LogManager(_logWindow);
                 _logger = new GenericLogger(_logManager.WriteLog);
                 _schemaSqlInit = new SqliteSchemaSqlInit(_logger);
                 _schemaProvider = new SqliteGetSchemaProvider
                    (
                         _schemaSqlInit,
                         _sqliteConnection,
                         _logger
                     );
               
                _tableSchema = new UsersTable().Build();
                _sqliteSchemaAdapter = new SqliteSchemaAdapter();
                _userSqlGen = new SqliteUserSqlGen
                    (
                        _tableSchema,
                        _sqliteSchemaAdapter,
                        _logger
                    );

                _logger.LogInformation($"Путь бд {dbPath}");
                 
                _logger.LogInformation("Application started. PID: " + Process.GetCurrentProcess().Id);

                _databaseInitializer = new DBTableInitializer
                    (
                        _schemaProvider,
                        _sqliteConnection,
                        _schemaSqlInit,
                        _tableSchema,
                        _logger
                    );

                // Асинхронное ожидание инициализации БД
                await _databaseInitializer.InitializeAsync();

                var repo = new UserRepository
                    (
                        _sqliteConnection,
                        _logger,
                        _userSqlGen
                    );

               

                _userService = new UserService(repo, _logger);

                // Создание ViewModel
                _mainVm = new MainViewModel(_userService, _logger);
                _status = (IStatusUpdater)_mainVm;
                DataContext = _status;

                // Асинхронная загрузка данных
                await LoadUsersOnStartupAsync();


                _logWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _logWindow.Left = Application.Current.MainWindow.Left + 1000;
                _logWindow.Top = Application.Current.MainWindow.Top + 10;


                _logWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Инициализация завершилась с ошибкой: {ex.Message}"); Close();
                throw; // Передаём исключение в ContinueWith
            }
        }

        // Асинхронный метод загрузки данных
        private async Task LoadUsersOnStartupAsync()
        {
            try
            {
                await _mainVm.LoadUsersAsync();
                _logger.LogInformation("Пользователи загружены при старте.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка загрузки пользователей при старте: " + ex.Message);
                MessageBox.Show(
                    $"Не удалось загрузить пользователей: {ex.Message}",
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

    }
}