using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetOrm.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace SiberNet.UI
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        // Один легитимный локатор для всего приложения — смотрит на монолитный куб!
        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        // Прямой доступ к шине берётся из единого провайдера
        public IEventBus EventBus => _serviceProvider?.GetRequiredService<IEventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Dapper.SqlMapper.AddTypeHandler(new HomeNetOrm.Helpers.DateTimeOffsetHandler());
                BackendMode currentMode = BackendMode.Real;

                // 1. Строки подключения
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";
                string postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

                // 2. 🔥 ВЫЗЫВАЕМ НАШ ОБЪЕДИНЕННЫЙ СБОРЩИК:
                // Он сам внутри себя запустит бэкграунд-прогрев баз и стартанет SiberNetSyncCoordinator!
                _serviceProvider = AppBootstrapper.Build(currentMode, postgresConnectionString, sqliteConnectionString);

                _serviceProvider.GetRequiredService<ILogQueueManager>();

                // 3. Открываем главное окно (или что у тебя тут запускает UI)
                // var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                // mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический сбой инициализации:\n\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 🧼 АВТО-ЧИСТКА: Контейнер сам сделает Dispose всем синглтонам!
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}
