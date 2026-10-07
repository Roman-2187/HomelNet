using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNetAvalonia.Infrastructure; // Наш бутстраппер и хаб
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
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

               
                // 🔥 ШАГ 4: БЕЗОПАСНЫЙ ФОНОВЫЙ ПУСК ОБЕИХ СУБД ОДНОВРЕМЕННО!
                if (currentMode == BackendMode.Real)
                {
                    var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 🔥 МЕНЯЕМ НА НАШ ДВУСТВОЛЬНЫЙ МЕТОД!
                            // Он параллельно запустит SQLite и Postgres, проверит структуры
                            // и выстрелит DatabaseReady в шину событий.
                            await dbCore.InitializeAllDatabasesAsync();

                            var logger = _serviceProvider.GetRequiredService<HomeNetCore.Interfaces.Diagnostics.ILogger>();
                            logger.LogInfo("[App Старт] Инициализация и параллельный запуск СУБД завершены. Прогреваем вьюмодели...");

                            // Прогреваем вьюшки админки и таблиц
                            _ = _serviceProvider.GetRequiredService<TableUsersViewModel>();
                            _ = _serviceProvider.GetRequiredService<DeleteUsersViewModel>();
                        }
                        catch (Exception ex)
                        {
                            var logger = _serviceProvider.GetRequiredService<HomeNetCore.Interfaces.Diagnostics.ILogger>();
                            logger.LogError($"[КРИТИЧЕСКИЙ КРАШ ПРИ ПАРАЛЛЕЛЬНОМ СТАРТЕ БАЗ]: {ex.Message}");
                        }
                    });
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
