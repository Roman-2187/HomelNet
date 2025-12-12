using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Enums;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Helpers;
using HomeNetCore.Services;
using System.Data.Common;
using System.Windows;
using WpfHomeNet.Controls;
using WpfHomeNet.Interfaces;
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

        private LogQueueManager LogQueueManager=> _logQueueManager ?? throw new InvalidOperationException("_logQueueManager не инициализирован");
        private LogQueueManager? _logQueueManager;

        private IUserRepository UserRepository =>_userRepository ?? throw new InvalidOperationException("_userRepository? не инициализирован");
        private UserRepository? _userRepository;

        private RegistrationViewControl _registrationControl;


        private DbConnection? _connection;
        private DBInitializer? _databaseInitializer;
        private ISchemaProvider? _schemaProvider;
        private ISchemaSqlInitializer? _schemaSqlInit;
        private TableSchema? _tableSchema;
        private ISchemaUserSqlGenerator? _userSqlGen;
        private ISchemaAdapter? _schemaAdapter;
        
        
        private RegistrationViewModel _registrationViewModel;
       
        
        #endregion


        public MainWindow()
        {
            InitializeComponent();
           

            InitializeLogging();

            CenterMainAndHideLogs();


        }

        
       
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            try
            {
               
                await InitializeAsync(DatabaseType.SQLite);

                await  InitializeRegistrationControlAsync();

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
      
    }
}