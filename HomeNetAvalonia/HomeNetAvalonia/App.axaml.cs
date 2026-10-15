using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNet.DI;
using HomeNetAvalonia.Infrastructure;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
using System;
using HomeNetCore.Extensions;

namespace HomeNetAvalonia
{
    public partial class App : Application
    {
        // Храним ссылку на наш ручной конструктор вместо IServiceProvider
        private AppCompositionRoot? _compositionRoot;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                BackendMode currentMode = BackendMode.Real;

                // 1. Находим пути к базам
                string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
                string sqliteConnectionString = $"Data Source={dbPath}";

                // СТАЛО: Передаем сетевой адрес нашего ASP.NET Core API сервера
                string apiBaseUrl = "http://localhost:5210"; // <--- Ставим порт 5210 вместо 5000!


                // 2. Запускаем наш "тупой" сборщик на чистом "new"
                _compositionRoot = AvaloniaUiBootstrapper.BuildAvaloniaContainer(currentMode, apiBaseUrl, sqliteConnectionString);


                // 2. Запускаем наш "тупой" сборщик на чистом "new"
                _compositionRoot = AvaloniaUiBootstrapper.BuildAvaloniaContainer(currentMode, apiBaseUrl, sqliteConnectionString);




                // 🔥 ШАГ 3: НАМЕРТВО ИНИЦИАЛИЗИРУЕМ ОКНО И ДАЁМ ЕМУ ВЬЮМОДЕЛЬ НАПРЯМУЮ!
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new Views.MainWindow
                    {
                        // Просто забираем MainViewModel из ручного графа! Без всяких GetRequiredService
                        DataContext = _compositionRoot.GetViewModel<MainViewModel>()
                    };
                }

                // 🔥 ШАГ 4: БЕЗОПАСНЫЙ ФОНОВЫЙ ПУСК ОБЕИХ СУБД И СИНХРОНИЗАТОРА!
                // Поскольку AppCompositionRoot при вызове Initialize() сам запускает фоновую таску 
                // прогрева DbContextContainer и SyncCoordinator, нам здесь даже писать ничего не нужно!
                // Но для логирования старта оставим простое уведомление:

                if (currentMode == BackendMode.Real)
                {
                    _compositionRoot.Logger.LogInfo("[App Старт] Двуствольный запуск СУБД движков и координатора репликации успешно запущен в фоне.");
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
