using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNetAvalonia.Infrastructure; // Наш бутстраппер и хаб
using HomeNetCore.Enums;
using HomeNetCore.Extensions;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.HomeNetOrm.Interfaces;
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
                // 🔥 ШАГ 4: БЕЗОПАСНЫЙ ФОНОВЫЙ ПРОГРЕВ И СИНХРОНИЗАЦИЯ СУБД ОДНОВРЕМЕННО!
                if (currentMode == BackendMode.Real)
                {
                    // Берём наш Хаб и Координатор строго по интерфейсам из DI
                    var dbCore = _serviceProvider.GetRequiredService<IDbContextContainer>();
                    var syncCoordinator = _serviceProvider.GetRequiredService<ISiberNetSyncCoordinator>();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Поскольку AppBootstrapper.Build уже пнул инициализацию баз на старте,
                            // мы просто дожидаемся завершения прогрева каналов и запускаем координатор репликации!
                            await dbCore.InitializeAllDatabasesAsync();
                            await syncCoordinator.StartAsync();

                            var logger = _serviceProvider.GetRequiredService<HomeNetCore.Interfaces.Diagnostics.ILogger>();
                            logger.LogInfo("[App Старт] Двуствольный запуск СУБД и координатора репликации SiberNet успешно выполнен. Прогреваем вьюмодели...");

                            // Мягко прогреваем ленивые вьюшки админки
                            _ = _serviceProvider.GetRequiredService<TableUsersViewModel>();
                            _ = _serviceProvider.GetRequiredService<DeleteUsersViewModel>();
                        }
                        catch (Exception ex)
                        {
                            var logger = _serviceProvider.GetRequiredService<HomeNetCore.Interfaces.Diagnostics.ILogger>();
                            logger.LogError($"[КРИТИЧЕСКИЙ КРАШ ПРИ ПАРАЛЛЕЛЬНОМ СТАРТЕ БАЗ И СИНХРОНИЗАЦИИ]: {ex.Message}");
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
