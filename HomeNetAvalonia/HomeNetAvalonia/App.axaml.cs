using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNet.DI;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetAvalonia.Infrastructure;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
using Microsoft.Extensions.Configuration;
using System;

namespace HomeNetAvalonia
{
    public partial class App : Application
    {
        private AppCompositionRoot? _compositionRoot;

        public IEventBus EventBus { get; private set; } = null!;

        public static readonly System.Diagnostics.Stopwatch AppStopwatch = System.Diagnostics.Stopwatch.StartNew();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            AppStopwatch.Restart();

            try
            {
                // 1. Читаем конфиг (appsettings.json + appsettings.{ENV}.json)
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build();

                // 2. Берём значения (если нет — кидаем исключение, чтобы не было "молчаливого" хардкода)
                string apiBaseUrl = config["ApiBaseUrl"]
                    ?? throw new InvalidOperationException("В appsettings.json отсутствует ApiBaseUrl");

                string databaseName = config["DatabaseName"]
                    ?? throw new InvalidOperationException("В appsettings.json отсутствует DatabaseName");

                string backendModeStr = config["BackendMode"]
                    ?? throw new InvalidOperationException("В appsettings.json отсутствует BackendMode");

                BackendMode currentMode = Enum.Parse<BackendMode>(backendModeStr);

                // 3. Путь к базе (только имя, путь строится внутри)
                string dbPath = DatabasePathHelper.GetDatabasePath(databaseName);
                string sqliteConnectionString = $"Data Source={dbPath}";

                // 4. Запускаем сборщик
                _compositionRoot = AvaloniaUiBootstrapper.BuildAvaloniaContainer(
                    currentMode,
                    apiBaseUrl,
                    sqliteConnectionString);

                EventBus = _compositionRoot.EventBus;

                // 5. Инициализируем окно и даём ему ViewModel
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new Views.MainWindow
                    {
                        DataContext = _compositionRoot.GetViewModel<MainViewModel>()
                    };

                    _compositionRoot.EventBusInspector.StartBackgroundScan(_compositionRoot.Logger);
                }

                if (currentMode == BackendMode.Real)
                {
                    _compositionRoot.Logger.LogInfo("[App Старт] запуск СУБД движков и координатора репликации успешно запущен в фоне.");
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