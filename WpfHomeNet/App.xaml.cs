using HomeNetCore.Data;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeNetCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using WpfHomeNet;
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers;
using WpfHomeNet.ViewModels;

namespace HomeSocialNetwork
{
    public partial class App : Application
    {
        #region Поля и свойства 
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _sqliteConnectionString = $"Data Source={dbPath}"; // Твоя SQLite строка 🔌

        private readonly string _postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

        private MainWindow? _mainWindow;
        private IServiceProvider? _serviceProvider;

        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        public EventBus EventBus => _serviceProvider?.GetRequiredService<EventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");
        #endregion

       

protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            Debug.WriteLine("DI-контейнер успешно создан");

            var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();

            Task.Run(async () =>
            {
                try
                {
                    var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();

                    // ШАГ А: Сначала дефолтный старт на PostgreSQL
                    await dbCore.InitializeAsync(DatabaseType.PostGreSQL);
                    Debug.WriteLine("База данных PostgreSQL успешно инициализирована.");

                    // =================================================================
                    // 🔥 ШАГ А.1: ПОДКЛЮЧАЕМ ДАТЧИК СЕТИ СРАЗУ ПОСЛЕ СТАРТА POSTGRES!
                    // =================================================================
                    NetworkChange.NetworkAddressChanged += async (sender, args) =>
                    {
                        // Проверяем, есть ли вообще живое подключение к интернету
                        bool isNetworkUp = NetworkInterface.GetIsNetworkAvailable();

                        if (!isNetworkUp && dbCore.CurrentType == DatabaseType.PostGreSQL)
                        {
                            // 📉 СЕТЬУПАЛА! Переключаем всю систему на SQLite за миллисекунды!
                            await dbCore.SwitchDatabaseAsync(DatabaseType.SQLite);

                            // Пинаем интерфейс через шину, чтобы статус-бар и таблицы обновились
                            var eventBus = _serviceProvider.GetRequiredService<EventBus>();
                            eventBus.Publish(new StatusTextChangedMessage("⚠️ Соединение потеряно! Мессенджер переведен в офлайн-режим (SQLite)."));
                            eventBus.Publish(new RequestStatusRefreshMessage());
                        }
                        else if (isNetworkUp && dbCore.CurrentType == DatabaseType.SQLite)
                        {
                            // 📈 СЕТЬ ВЕРНУЛАСЬ! Возвращаем Enterprise на рельсы PostgreSQL!
                            await dbCore.SwitchDatabaseAsync(DatabaseType.PostGreSQL);

                            var eventBus = _serviceProvider.GetRequiredService<EventBus>();
                            eventBus.Publish(new StatusTextChangedMessage("⚡ Сеть восстановлена. Синхронизация с PostgreSQL успешна!"));
                            eventBus.Publish(new RequestStatusRefreshMessage());
                        }
                    };
                    // =================================================================

                    // ШАГ Б: Когда базовая готовность подтверждена — будим UI вью-модели
                    await Current.Dispatcher.InvokeAsync(() =>
                    {
                        _serviceProvider.GetRequiredService<UsersTableViewModel>();
                        _serviceProvider.GetRequiredService<UserDashboardViewModel>();
                        // 🔥 Будим и чат на всякий случай
                        _serviceProvider.GetRequiredService<ChatViewModel>();

                        _serviceProvider.GetRequiredService<AdminMenuViewModel>();
                        _serviceProvider.GetRequiredService<RegistrationViewModel>();
                        _serviceProvider.GetRequiredService<AuthenticationViewModel>();

                        // ШАГ В: Открываем главное окно
                        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                        


                        // 🛸 ТРЮК С ТЕЛЕПОРТАЦИЕЙ: Сразу вычисляем позицию под монитором
                        double screenHeight = SystemParameters.PrimaryScreenHeight;
                        double screenWidth = SystemParameters.PrimaryScreenWidth;

                        _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual; // Отключаем авто-центр
                        _mainWindow.Left = (screenWidth - _mainWindow.Width) / 2;       // Центрируем по горизонтали
                        _mainWindow.Top = screenHeight + 100;                           // Прячем глубоко под нижний край

                        // Окно рендерится невидимым для пользователя там, где его никто не видит
                        _mainWindow.Show();
                    });
                }
                catch (Exception ex)
                {
                    await Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Ошибка инициализации БД:\n\n{ex.ToString()}", "Критический сбой", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    });
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Критическая ошибка запуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }


        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<EventBus>();
            services.AddSingleton<StatusBarViewModel>();

            // 🔥 ИСПРАВЛЕНО: Убрали LogWindow. Менеджер теперь автономен и просто копит логи в очередь
            services.AddSingleton<LogQueueManager>(provider =>
            {
                var manager = new LogQueueManager(5); // Передаем только размер пачки, без старого окна
                provider.GetRequiredService<ILogger>().SetOutput(manager.WriteLog);
                return manager;
            });

            // 2. Передаем в контейнер обе строки подключения
            services.AddSingleton(provider =>
                new DbContextContainer(_postgresConnectionString, _sqliteConnectionString, provider.GetRequiredService<ILogger>()));

            // Регистрация репозиториев данных
            services.AddSingleton<HomeNetCore.Data.Repositories.UserRepository>();
            services.AddSingleton<HomeNetCore.Data.Repositories.MessageRepository>();
            services.AddSingleton<HomeNetCore.Data.Repositories.FriendRepository>();

            // Бизнес-сервисы
            services.AddSingleton<UserService>();
            services.AddSingleton<RegisterService>();
            services.AddSingleton<AuthenticateService>();
            services.AddSingleton<DeleteService>();
            services.AddSingleton<MessageService>();
            services.AddSingleton<FriendService>();

            // 3. Регистрация Вьюмоделей
            services.AddSingleton<UsersTableViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<AdminMenuViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<UserDashboardViewModel>();
            services.AddSingleton<ChatViewModel>();

            services.AddSingleton(provider =>
            {
                return new MainViewModel(
                    provider.GetRequiredService<ILogger>(),
                    provider.GetRequiredService<EventBus>());
            });

            services.AddTransient<MainWindow>();
        }

    }
}
