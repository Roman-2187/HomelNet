using HomeNetCore.Data;
using HomeNetCore.Data.Enums;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfHomeNet.Controls;
using WpfHomeNet.Interfaces;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    public partial class MainWindow
    {       

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


                // Создаём VM регистрации (ещё без контрола)
                _registrationViewModel = new RegistrationViewModel(_userRepository);

                // Создание ViewModel
                _mainVm = new MainViewModel(UserService, Logger,_registrationViewModel);
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


       
        private async Task InitializeRegistrationControlAsync()
        {
            if (UserRepository == null)
                throw new InvalidOperationException("UserRepository не инициализирован!");

            if (_registrationViewModel == null)
                throw new InvalidOperationException("RegistrationViewModel не инициализирована!");
          
            _registrationControl = new RegistrationViewControl();

            MainGrid.Children.Add(_registrationControl);
            Grid.SetColumn(_registrationControl, 1);
            Grid.SetColumnSpan(_registrationControl, 4);
            Grid.SetRow(_registrationControl, 1);
            Grid.SetRowSpan(_registrationControl, 5);

            // 3. Назначаем DataContext
            _registrationControl.DataContext = _registrationViewModel;

            // 4. Настраиваем привязку Visibility
            var binding = new Binding("ControlVisibility")
            {
                Source = _registrationViewModel
            };
            _registrationControl.SetBinding(UIElement.VisibilityProperty, binding);
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



        private void InitializeLogging()
        {
            _logger = new Logger();
            _logWindow = new LogWindow(Logger);
            _logQueueManager = new LogQueueManager(LogWindow, 20);
           
           Logger.SetOutput(_logQueueManager.WriteLog);

            Logger.LogInformation($"Путь БД: {dbPath}");
            Logger.LogInformation("Application started. PID: " + Process.GetCurrentProcess().Id);
        }



        private void CenterMainAndHideLogs()
        {
            this.Left = 200;
            this.Top = 200;
            btnLogs.Content = "Показать логи";
        }

    }
}