using HomeNet.DI;                 // Подключаем наш новый чистый слой сборщика!
using HomeNetCore.Enums;           // Твой новый enum режимов бэкенда!
using HomeNetCore.Events;
using HomeNetCore.Interfaces;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;            // Или где у тебя лежит DatabaseType
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Diagnostics; // Путь к твоей логике LogQueueManager
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;

namespace SiberNet.UI
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
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

                // 2. 🔥 ШАГ 1: Собираем универсальный DI-куб ОДНОЙ строчкой во внешнем слое!
                // Передаем режим (Real/WithMocks) и строки подключения
                _serviceProvider = AppBootstrapper.Build(BackendMode.Real, postgresConnectionString, sqliteConnectionString);
                Debug.WriteLine("Универсальный DI-контейнер успешно собран через HomeNet.DI.");

                // 3. Будим базовую инфраструктуру
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();
                var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();

                // Честно ждем инициализации БД через await без всяких Task.Run!
                await dbCore.InitializeAsync(DatabaseType.PostGreSQL);
                Debug.WriteLine("База данных PostgreSQL успешно инициализирована.");

                // 4. Датчик сети (оставляем его тут, так как NetworkChange — штука системная)
                NetworkChange.NetworkAddressChanged += async (sender, args) =>
                {
                    bool isNetworkUp = NetworkInterface.GetIsNetworkAvailable();
                    var eventBus = _serviceProvider.GetRequiredService<IEventBus>();

                    if (!isNetworkUp && dbCore.CurrentType == DatabaseType.PostGreSQL)
                    {
                        await dbCore.SwitchDatabaseAsync(DatabaseType.SQLite);
                        eventBus.Publish(this, new StatusTextChangedMessage("⚠️ Соединение потеряно! Мессенджер переведен в офлайн-режим (SQLite)."));
                        eventBus.Publish(this, new RequestStatusRefreshMessage());
                    }
                    else if (isNetworkUp && dbCore.CurrentType == DatabaseType.SQLite)
                    {
                        await dbCore.SwitchDatabaseAsync(DatabaseType.PostGreSQL);
                        eventBus.Publish(this, new StatusTextChangedMessage("⚡ Сеть восстановлена. Синхронизация с PostgreSQL успешна!"));
                        eventBus.Publish(this, new RequestStatusRefreshMessage());
                    }
                };

                // 🔥 ШАГ 2-3 в одном флаконе: достаем MainViewModel и сразу суем в конструктор окна!
                _mainWindow = new MainWindow(AppBootstrapper.GetViewModel<MainViewModel>());



                // Поехали! Приложение живет, пока открыто окно
                _mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический сбой инициализации:\n\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
