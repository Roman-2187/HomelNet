using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNetAvalonia.Infrastructure; // Наш бутстраппер и хаб
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels.AdminViews;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace HomeNetAvalonia
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                BackendMode currentMode = BackendMode.Real;

                // 1. Строки подключения
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";
                string postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

                // 2. Вызываем наш Авалон-сборщик контейнера (БЕЗ запуска базы внутри!)
                _serviceProvider = AvaloniaUiBootstrapper.BuildAvaloniaContainer(currentMode, postgresConnectionString, sqliteConnectionString);

                // 🔥 ШАГ 3 (БЫВШИЙ 4): СНАЧАЛА НАМЕРТВО ИНИЦИАЛИЗИРУЕМ ОКНО!
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new Views.MainWindow
                    {
                        DataContext = _serviceProvider.GetRequiredService<HomeNetPresentation.ViewModels.MainViewModel>()
                    };
                }

                // 🔥 ШАГ 4 (БЫВШИЙ 3): БЕЗОПАСНЫЙ ФОНОВЫЙ ПУСК СУБД ПОСЛЕ ТОГО, КАК ОКНО ОТКРЫЛОСЬ!
                if (currentMode == BackendMode.Real)
                {
                    var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();
                   

                    // Запускаем через фоновую задачу, чтобы UI-поток Авалонии дышал свободно!
                    // Логи инициализации Postgres побегут прямо в твой терминал на экране!
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await dbCore.InitializeAsync(DatabaseType.PostGreSQL);
                        }
                        catch (Exception ex)
                        {
                            var logger = _serviceProvider.GetRequiredService<HomeNetCore.Interfaces.Diagnostics.ILogger>();
                            logger.LogError($"[КРАШ СУБД В ВЕБЕ/ДЕСКТОПЕ]: {ex.Message}");
                        }
                    });
                }


                // 🔥 2. ОКНО ГОТОВО, ДИСПЕТЧЕР СТАРТАНУЛ — ТЕПЕРЬ БЕЗОПАСНО БУДИМ ТАБЛИЦУ
                
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
