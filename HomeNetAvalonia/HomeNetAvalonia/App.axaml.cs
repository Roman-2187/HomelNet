using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNetAvalonia.Infrastructure; // Наш бутстраппер и хаб
using HomeNetCore.Enums;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HomeNetAvalonia
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            try
            {
                BackendMode currentMode = BackendMode.Real;

                // 1. Строки подключения (Один в один твоя логика)
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";
                string postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

                // 2. 🔥 ВЫЗЫВАЕМ НАШ АВАЛОНИЯ-СБОРЩИК
                _serviceProvider = AvaloniaUiBootstrapper.BuildAvaloniaContainer(currentMode, postgresConnectionString, sqliteConnectionString);

                // 3. Будим базы данных, если у нас боевой режим
                if (currentMode == BackendMode.Real)
                {
                    var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();
                    await dbCore.InitializeAsync(DatabaseType.SQLite);
                }

                // 4. Запуск главного окна
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new Views.MainWindow
                    {
                        DataContext = HomeNet.DI.AppBootstrapper.GetViewModel<HomeNetPresentation.ViewModels.MainViewModel>()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критический сбой инициализации в Авалонии:\n\n{ex.Message}");
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
