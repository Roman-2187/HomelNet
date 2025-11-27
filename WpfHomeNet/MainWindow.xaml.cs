using HomeNetCore.Data;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Data.PostgreClasses;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Data.Schemes.GetSchemaTableBd;
using HomeNetCore.Data.SqliteClasses;
using HomeNetCore.Data.TableSchemas;
using HomeNetCore.Helpers;
using HomeNetCore.Services;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using WpfHomeNet.Data.DBProviders.Postgres;
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

        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");        
        private  readonly string _connectionString =$"Data Source={dbPath}";        
        private DbConnection? _connection;
        private DBTableInitializer? _databaseInitializer;
        private ISchemaProvider? _schemaProvider;
        private ISchemaSqlInitializer? _schemaSqlInit;
        private TableSchema? _tableSchema;        
        public LogWindow? _logWindow;
        private UserService? _userService;
        private MainViewModel? _mainVm;
        private IStatusUpdater? _status;
        private ILogger? _logger;
        private IUserSqlGenerator? _userSqlGen;
        private ISchemaAdapter? _schemaAdapter;
        private LogQueueManager? _logQueueManager;
        private UserRepository? _userRepository;
        

        public MainWindow()
        {
            InitializeComponent(); 
        
            InitializeLogging();

            CenterMainAndHideLogs();         
        }




        private void InitializeLogging()
        {
            _logger = new Logger();
            _logWindow = new LogWindow(_logger);
            _logQueueManager = new LogQueueManager(_logWindow);
            _logger.SetOutput(_logQueueManager.WriteLog);

            _logger.LogInformation($"Путь БД: {dbPath}");
            _logger.LogInformation("Application started. PID: " + Process.GetCurrentProcess().Id);
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
                _logger?.LogError($"Критическая ошибка при запуске: {ex.Message}");
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

                var factory = new DatabaseServiceFactory(_connectionString, _logger);

                // 2. Получаем все сервисы одним вызовом
                var (connection, sqlInit, schemaProvider, schemaAdapter, userSqlGen) =
                    factory.CreateServices(databaseType, _tableSchema);


                // 3. Сохраняем в поля класса (если нужно)
                _connection = connection;
                _schemaSqlInit = sqlInit;
                _schemaProvider = schemaProvider;
                _schemaAdapter = schemaAdapter;
                _userSqlGen = userSqlGen;



                _databaseInitializer = new DBTableInitializer(
                    _connection, _schemaProvider,
                    _schemaAdapter, _schemaSqlInit,
                    _tableSchema, _logger);

                // Асинхронное ожидание инициализации БД
                await _databaseInitializer.InitializeAsync();

                _userRepository = new UserRepository(_connection, _logger, _userSqlGen);

                _userService = new UserService(_userRepository, _logger);

                // Создание ViewModel
                _mainVm = new MainViewModel(_userService, _logger); 
                
                         
            }
            catch (Exception ex)
            {

                _logger?.LogError($"Инициализация завершилась с ошибкой: {ex.Message}");

                CloseAllWindows();

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
            if (_logger is null)
            {
                throw new InvalidOperationException("_logger не инициализирован");
            }

            if (_logQueueManager is null)
            {
                throw new InvalidOperationException("_logQueueManager не инициализирован");
            }
                      
            _status = (IStatusUpdater?)_mainVm;

            DataContext = _status;            
        }

        private async Task LoadUsersOnStartupAsync()
        {
            try
            {
                if (_mainVm != null)
                {
                    await _mainVm.LoadUsersAsync();
                   
                }
                else
                {
                    _logger?.LogError("MainViewModel не инициализирован");
                    throw new InvalidOperationException("MainViewModel не инициализирован");
                }

            }
            catch (Exception ex)
            {
                _logger?.LogError("Ошибка загрузки пользователей при старте: " + ex.Message);
                MessageBox.Show(
                    $"Не удалось загрузить пользователей: {ex.Message}",
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        private void CloseAllWindows()
        {
            // Получаем коллекцию окон
            foreach (Window window in Application.Current.Windows)
            {
                // Не закрываем главное окно
                if (window != this)
                {
                    window.Close();
                }
            }
        }

     
        private void ShowLogsAndShift(LogWindow logWindow)
        {
            // Позиция главного окна
            this.Left = 20;

            // Позиция окна логов
            logWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            logWindow.Left = this.Left + 1005;
            logWindow.Top = this.Top;
            logWindow.Show();

            btnLogs.Content = "Скрыть логи";
        }

        private void CenterMainAndHideLogs()
        {
            // Центрируем главное окно через SystemParameters
            this.Left = 150;
           this.Top = 200;

            btnLogs.Content = "Показать логи";
        }



    }
}