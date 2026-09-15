using Data.Repositories;
using HomeNetCore.Interfaces;                 // Чистые контракты интерфейсов из Ядра 🧼 UFO
using HomeNetCore.Messaging;                  // Чистые рекорды-сигналы из Ядра
using HomeNetOrm.Data.Builders;
using HomeNetOrm.Data.Repositories;
using HomeNetOrm.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Diagnostics;
using HomeNetServices.Routing;
using HomeNetServices.Services.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;


// Твой логгер и менеджер очередей



namespace SiberNet.UI
{
    public partial class App : Application
    {
        #region Поля и свойства 
        private static readonly string dbPath = DatabasePathHelper.GetDatabasePath("home_net.db");
        private readonly string _sqliteConnectionString = $"Data Source={dbPath}";

        private readonly string _postgresConnectionString = "Server=127.0.0.1:5432;Database=home_net_db;User Id=postgres;Password=05011987;";

        private MainWindow? _mainWindow;
        private IServiceProvider? _serviceProvider;

        public IServiceProvider Services => _serviceProvider
           ?? throw new InvalidOperationException("Провайдер не инициализирован");

        // 🔥 ИСПРАВЛЕНО: Теперь возвращает чистый интерфейс IEventBus из Ядра!
        public IEventBus EventBus => _serviceProvider?.GetRequiredService<IEventBus>()
            ?? throw new InvalidOperationException("Провайдер сервисов не инициализирован");
        #endregion

        // 🔥 ШАГ 1: Добавили ключевое слово async в подпись метода! 🧼 UFO
        protected override async void OnStartup(StartupEventArgs e)
        {

            

            base.OnStartup(e);

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                Debug.WriteLine("DI-контейнер успешно создан");

                // Будим менеджер очередей логов
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();
                var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();

                // 🔥 ШАГ 2: УБРАЛИ Task.Run! Теперь честно ждем инициализации через await! 🦾⚡
                // Приложение НЕ закроется, пока эта строчка не отработает
                await dbCore.InitializeAsync(DatabaseType.PostGreSQL);
                Debug.WriteLine("База данных PostgreSQL успешно инициализирована.");

                // Подключаем датчик отслеживания сети интернет
                NetworkChange.NetworkAddressChanged += async (sender, args) =>
                {
                    bool isNetworkUp = NetworkInterface.GetIsNetworkAvailable();

                    if (!isNetworkUp && dbCore.CurrentType == DatabaseType.PostGreSQL)
                    {
                        await dbCore.SwitchDatabaseAsync(DatabaseType.SQLite);
                        var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
                        eventBus.Publish(this, new StatusTextChangedMessage("⚠️ Соединение потеряно! Мессенджер переведен в офлайн-режим (SQLite)."));
                        eventBus.Publish(this, new RequestStatusRefreshMessage());
                    }
                    else if (isNetworkUp && dbCore.CurrentType == DatabaseType.SQLite)
                    {
                        await dbCore.SwitchDatabaseAsync(DatabaseType.PostGreSQL);
                        var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
                        eventBus.Publish(this, new StatusTextChangedMessage("⚡ Сеть восстановлена. Синхронизация с PostgreSQL успешна!"));
                        eventBus.Publish(this, new RequestStatusRefreshMessage());
                    }
                };

                // Будим наши ВьюМодели в контейнере зависимостей
                _serviceProvider.GetRequiredService<UsersTableViewModel>();
                _serviceProvider.GetRequiredService<UserDashboardViewModel>();
                _serviceProvider.GetRequiredService<ChatViewModel>(); 
                _serviceProvider.GetRequiredService<RegistrationViewModel>();
                _serviceProvider.GetRequiredService<AuthenticationViewModel>();

                // 🔥 ШАГ 3: Создаем и показываем наше главное окно!
                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                //// Настройка геометрии анимации окна при старте
                //double screenHeight = SystemParameters.PrimaryScreenHeight;
                //double screenWidth = SystemParameters.PrimaryScreenWidth;

                //_mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                //_mainWindow.Left = (screenWidth - _mainWindow.Width) / 2;
                //_mainWindow.Top = screenHeight + 100; // Позиция под экраном для вылета

                _mainWindow.Show(); // Окно на экране, WPF спокоен и держит приложение живым! 🛸✨
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический сбой инициализации:\n\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }


        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Системная инфраструктура (Singleton)
            services.AddSingleton<ILogger, Logger>();

            // 🔥 КЛЮЧЕВОЙ МОСТ: Связываем интерфейс Ядра с нашей новой реализацией Логистики! 🧼🦾
            services.AddSingleton<IEventBus, EventBus>();

            services.AddSingleton<StatusBarViewModel>();

            // Настройка менеджера очередей логов (по умолчанию 0 задержки для скорости!)
            services.AddSingleton<LogQueueManager>(provider =>
            {
                var manager = new LogQueueManager(0);

                // Привязываем наш Logger из Сервисов напрямую к методу WriteLog менеджера очередей
                provider.GetRequiredService<ILogger>().SetOutput((msg, level) => manager.WriteLog(msg, level));

                return manager;
            });

            // 2. Передаем в контейнер обе строки подключения
            services.AddSingleton(provider =>
                new DbContextContainer(_postgresConnectionString, _sqliteConnectionString, provider.GetRequiredService<ILogger>()));

            // Регистрация репозиториев данных
            services.AddSingleton<IUserRepository, UserRepository>();


            services.AddSingleton<FriendRepository>();
            // Было: services.AddSingleton<MessageRepository>();
            // Стало: склеиваем интерфейс Ядра с живым классом ОРМ! 🧼🛸
            services.AddSingleton<IMessageRepository, MessageRepository>();

            // Бизнес-сервисы из нашего нового чистого домена Identity! 🔒🛸
            services.AddSingleton<IUserService, UserService>(); // Живой бэкенд базы!




            services.AddSingleton<RegisterService>();
            services.AddSingleton<AuthenticateService>();
            services.AddSingleton<DeleteService>();
            services.AddSingleton<MessageService>();
            services.AddSingleton<FriendService>();

            // 3. Регистрация Вьюмоделей слоя Презентации
            services.AddSingleton<UsersTableViewModel>();
            services.AddSingleton<RegistrationViewModel>();
            services.AddSingleton<AuthenticationViewModel>();
            services.AddSingleton<AdminMenuViewModel>();
            services.AddSingleton<DeleteUsersViewModel>();
            services.AddSingleton<UserDashboardViewModel>();
            services.AddSingleton<ChatViewModel>();
            // 🔥 РЕГИСТРИРУЕМ ЦЕНТРАЛЬНЫЙ УЗЕЛ ПРЕЗЕНТАЦИИ, ЧТОБЫ ОКНО ПРОЗРЕЛО! 🧼🛸
            services.AddTransient<HomeNetPresentation.ViewModels.MainViewModel>();




            services.AddTransient<MainWindow>();
        }
    }
}
