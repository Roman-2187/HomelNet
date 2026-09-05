using HomeNetCore.Data;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Services;
using HomeNetCore.Services.AuthenticationService;
using HomeNetCore.Services.DeleteService;
using HomeNetCore.Services.ListUsersServise;
using System.Data.Common;

namespace HomeSocialNetwork.Core
{
    public class DbInfrastructureCore
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        // Внутреннее соединение
        private DbConnection? _connection;

        // 👤 Сервисы пользователей
        public UserService UserService { get; private set; } = null!;
        public ListUsersService ListUsersService { get; private set; } = null!;
        public RegisterService RegisterService { get; private set; } = null!;
        public AuthenticateService AuthenticateService { get; private set; } = null!;
        public DeleteService DeleteService { get; private set; } = null!;

        // 💬 Сервисы чата и контактов семьи
        public MessageService MessageService { get; private set; } = null!;
        public FriendService FriendService { get; private set; } = null!;

        public DbInfrastructureCore(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(DatabaseType databaseType)
        {
            _logger.LogInformation("Запуск инициализации инфраструктуры БД...");

            // 1. Создаем фабрику СУБД
            var factory = new DatabaseServiceFactory(_connectionString, _logger);

            // 2. Вытаскиваем базовые компоненты из фабрики (без передачи каких-либо схем-заглушек!)
            var (connection, sqlInit, schemaProvider, schemaAdapter) = factory.CreateCoreInfrastructure(databaseType);
            _connection = connection;

            // 🔥 ЖЕСТКОЕ ИСПРАВЛЕНИЕ: Открываем сетевой шлейф к PostgreSQL перед проверкой метаданных!
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            // =================================================================
            // 🚂 АВТОПИЛОТ: НАКАТЫВАЕМ И СВЕРЯЕМ ВСЕ ТАБЛИЦЫ СРАЗУ
            // =================================================================
            // Наш новый DBInitializer сам пойдет в SchemaRegistry и все проверит за один вызов!
            var dbInitializer = new DBInitializer(_connection, schemaProvider, schemaAdapter, sqlInit,sqlInit ,_logger);
            await dbInitializer.InitializeAsync();

            // =================================================================
            // 🚀 ШТАМПУЕМ АВТОНОМНЫЕ ГЕНЕРАТОРЫ SQL ПОД КАЖДУЮ СУЩНОСТЬ
            // =================================================================
            // Никакой каши, никаких индексов [0] или. Фабрика сама создаст дженерик-генераторы,
            // а они сами заберут свои схемы из реестра.
            var userSqlGen = factory.CreateSqlGenerator<UserEntity>(databaseType, schemaAdapter);
            var messageSqlGen = factory.CreateSqlGenerator<MessageEntity>(databaseType, schemaAdapter);
            var friendSqlGen = factory.CreateSqlGenerator<FriendEntity>(databaseType, schemaAdapter);

            // =================================================================
            // СВЯЗЫВАЕМ РЕПОЗИТОРИИ И СЕРВИСЫ БИЗНЕС-ЛОГИКИ
            // =================================================================
            // Блок пользователей
            var userRepository = new UserRepository(_connection, userSqlGen);
            UserService = new UserService(userRepository, _logger);
            ListUsersService = new ListUsersService(UserService);
            RegisterService = new RegisterService(UserService);
            AuthenticateService = new AuthenticateService(UserService);
            DeleteService = new DeleteService(_logger, UserService);

            // Блок сообщений чата
            var messageRepository = new MessageRepository(_connection, messageSqlGen);
            MessageService = new MessageService(messageRepository, _logger);

            // Блок контактов/друзей
            var friendRepository = new FriendRepository(_connection, friendSqlGen);
            FriendService = new FriendService(friendRepository, _logger);

            _logger.LogInformation("Вся бизнес-логика ядра успешно переведена на дженерик-рельсы и запущена!");
        }
    }
}


