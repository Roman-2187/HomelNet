using HomeNetCore.Data;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeNetCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
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
        private readonly string _connectionString = $"Data Source={dbPath}";

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

                // 1. Сначала обязательно будим логгер, чтобы окно логов открылось!
                var kickLogger = _serviceProvider.GetRequiredService<LogQueueManager>();

                // 2. ХРОНОЛОГИЧЕСКИЙ ПОРЯДОК: Запускаем строго последовательную цепочку в фоне! 🚀
                Task.Run(async () =>
                {
                    try
                    {
                        var dbCore = _serviceProvider.GetRequiredService<DbContextContainer>();

                        // ШАГ А: Сначала ЖДЁМ пока Postgres полностью проверит и создаст таблицы
                        await dbCore.InitializeAsync(DatabaseType.PostGreSQL);

                        Debug.WriteLine("База данных PostgreSQL успешно инициализирована.");

                        // ШАГ Б: Только КОГДА БАЗА НА 100% ГОТОВА — возвращаемся в UI-поток и пинаем вью-модели! 💎
                        await Current.Dispatcher.InvokeAsync(() =>
                        {
                            // Прогреваем вью-модели, которые теперь качают данные напрямую
                            _serviceProvider.GetRequiredService<UsersTableViewModel>();
                            _serviceProvider.GetRequiredService<UserDashboardViewModel>();

                            _serviceProvider.GetRequiredService<AdminMenuViewModel>();
                            _serviceProvider.GetRequiredService<RegistrationViewModel>();
                            _serviceProvider.GetRequiredService<AuthenticationViewModel>();

                            // ШАГ В: Спокойно открываем и показываем главное окно мессенджера
                            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
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

            // Лог-менеджер
            services.AddSingleton(provider => new LogWindow(provider.GetRequiredService<ILogger>()));
            services.AddSingleton(provider =>
            {
                var logWin = provider.GetRequiredService<LogWindow>();
                var manager = new LogQueueManager(logWin, 5);
                provider.GetRequiredService<ILogger>().SetOutput(manager.WriteLog);
                return manager;
            });

            // 2. Регистрируем Контекст СУБД (Singleton)
            services.AddSingleton(provider =>
                new DbContextContainer(_postgresConnectionString, provider.GetRequiredService<ILogger>())); // Переименованный класс ядра

            // 🚀 НОВЫЙ ЭТАЖ: Регистрируем репозитории данных (Они берут коннект и генераторы прямо из DbContextContainer)
            services.AddSingleton(provider =>
            {
                var context = provider.GetRequiredService<DbContextContainer>();
                return new HomeNetCore.Data.Repositories.UserRepository(context.Connection, context.UserSqlGen);
            });

            services.AddSingleton(provider =>
            {
                var context = provider.GetRequiredService<DbContextContainer>();
                return new HomeNetCore.Data.Repositories.MessageRepository(context.Connection, context.MessageSqlGen);
            });

            services.AddSingleton(provider =>
            {
                var context = provider.GetRequiredService<DbContextContainer>();
                return new HomeNetCore.Data.Repositories.FriendRepository(context.Connection, context.FriendSqlGen);
            });

            // 🧠 НОВЫЙ ЭТАЖ: Регистрируем бизнес-сервисы в DI (Контейнер сам прокинет в них репозитории и логгер!)
            services.AddSingleton(provider =>
                new UserService(provider.GetRequiredService<HomeNetCore.Data.Repositories.UserRepository>(), provider.GetRequiredService<ILogger>()));

            services.AddSingleton(provider =>
                new RegisterService(provider.GetRequiredService<UserService>()));

            services.AddSingleton(provider =>
                new AuthenticateService(provider.GetRequiredService<UserService>()));

            services.AddSingleton(provider =>
                new DeleteService(provider.GetRequiredService<ILogger>(), provider.GetRequiredService<UserService>()));

            services.AddSingleton(provider =>
                new MessageService(provider.GetRequiredService<HomeNetCore.Data.Repositories.MessageRepository>(), provider.GetRequiredService<ILogger>()));

            services.AddSingleton(provider =>
                new FriendService(provider.GetRequiredService<HomeNetCore.Data.Repositories.FriendRepository>(), provider.GetRequiredService<ILogger>()));


            // 3. 🎨 Регистрация Вьюмоделей (Берут сервисы НАПРЯМУЮ ИЗ DI, без посредника core!)
            services.AddSingleton(provider =>
                new UsersTableViewModel(
                    provider.GetRequiredService<EventBus>(),
                    provider.GetRequiredService<UserService>() // Тянем прямо из DI! 💎
                ));

            services.AddSingleton(provider =>
                new RegistrationViewModel(
                    provider.GetRequiredService<RegisterService>(), // Из DI! 💎
                    provider.GetRequiredService<EventBus>()
                ));

            services.AddSingleton(provider =>
                new AuthenticationViewModel(
                    provider.GetRequiredService<AuthenticateService>(), // Из DI! 💎
                    provider.GetRequiredService<EventBus>()
                ));

            services.AddSingleton(provider =>
                new AdminMenuViewModel(
                    provider.GetRequiredService<UserService>(), // Из DI! 💎
                    provider.GetRequiredService<EventBus>()
                ));

            services.AddSingleton(provider =>
                new DeleteUsersViewModel(
                    provider.GetRequiredService<DeleteService>(), // Из DI! 💎
                    provider.GetRequiredService<EventBus>(),
                    provider.GetRequiredService<ILogger>()
                ));

            services.AddSingleton(provider =>
                new UserDashboardViewModel(provider.GetRequiredService<EventBus>()));

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
