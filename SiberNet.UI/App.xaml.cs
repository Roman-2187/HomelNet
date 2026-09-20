using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
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

        // Один легитимный локатор для всего приложения — смотрит на монолитный куб!
        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        // Прямой доступ к шине берётся из единого провайдера
        public IEventBus EventBus => _serviceProvider?.GetRequiredService<IEventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {

                // Наш глобальный рубильник режимов бэкенда (Real / Local / Mock)
                BackendMode currentMode = BackendMode.Real;

                // 1. Строки подключения
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";
                string postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

                // 2. 🔥 ВЫЗЫВАЕМ НАШ ОБЪЕДИНЕННЫЙ СБОРЩИК: Наследуем чертеж Ядра и цементируем с WPF-аниматорами
                _serviceProvider = WpfUiBootstrapper.BuildWpfContainer(currentMode, postgresConnectionString, sqliteConnectionString);

                // 3. Будим базы данных, если у нас боевой режим
                if (currentMode == BackendMode.Real)
                {
                    var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();
                    await dbCore.InitializeAsync(DatabaseType.PostGreSQL);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический сбой инициализации:\n\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 🧼 АВТО-ЧИСТКА: Контейнер сам сделает Dispose всем синглтонам-аниматорам окон!
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}
