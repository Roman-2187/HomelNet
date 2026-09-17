using HomeNet.DI;                 
using HomeNetCore.Enums;           
using HomeNetCore.Interfaces;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;           
using HomeNetOrm.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SiberNet.UI.Infrastructure;
using System.Windows;

namespace SiberNet.UI
{
    public partial class App : Application
    {
       
        private IServiceProvider? _serviceProvider;

        // Локатор для XAML (если где-то еще используется через App.Current.Services)
        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        // Прямой доступ к шине для датчика сети
        public IEventBus EventBus => _serviceProvider?.GetRequiredService<IEventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. Строки подключения (пока держим тут, потом утянем в json-конфиг)
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";
                string postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

                // 1. Собрали универсальный куб одной строчкой
                _serviceProvider = AppBootstrapper.Build(BackendMode.Real, postgresConnectionString, sqliteConnectionString);

                // 2. ВРУБАЕМ НАШ ИЗОЛИРОВАННЫЙ КЛАСС-АНИМАТОР
                var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
                var animator = new WindowAnimator(eventBus);

                // 3. Будим базы и логгер (твой старый код)...
                var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();
                await dbCore.InitializeAsync(DatabaseType.PostGreSQL);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический сбой инициализации:\n\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
