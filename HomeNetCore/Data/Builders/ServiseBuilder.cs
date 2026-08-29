using HomeNetCore.Data;
using HomeNetCore.Data.Adapters;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Data.Schemes;
using HomeNetCore.Enums;
using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using HomeNetCore.Services.AuthenticationService;
using HomeNetCore.Services.DeleteService;
using HomeNetCore.Services.ListUsersServise;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace HomeSocialNetwork.Core
{
    public class DbInfrastructureCore
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        // Все скрытые шестерёнки теперь инкапсулированы внутри и не торчат наружу!
        private DbConnection? _connection;
        private TableSchema? _tableSchema;
        private ISchemaUserSqlGenerator? _userSqlGen;
        private DBInitializer? _databaseInitializer;
        private UserRepository? _userRepository;

        // Открытые наружу чистые «детали», к которым будем обращаться через точку
        public UserService UserService { get; private set; } = null!;
        public ListUsersService ListUsersService { get; private set; } = null!;
        public RegisterService RegisterService { get; private set; } = null!;
        public AuthenticateService AuthenticateService { get; private set; } = null!;
        public DeleteService DeleteService { get; private set; } = null!;

        public DbInfrastructureCore(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Метод полной сборки детали под капотом
        public async Task InitializeAsync(DatabaseType databaseType)
        {
            _tableSchema = new UsersTable().Build();
            var factory = new DatabaseServiceFactory(_connectionString, _logger);

            var (connection, sqlInit, schemaProvider, schemaAdapter, userSqlGen) =
                factory.CreateServices(databaseType, _tableSchema);

            _connection = connection;
            _userSqlGen = userSqlGen;

            // Запускаем инициализацию таблиц и схем
            _databaseInitializer = new DBInitializer(
                _connection, schemaProvider, schemaAdapter,
                sqlInit, _tableSchema, _logger);

            await _databaseInitializer.InitializeAsync();
            _logger.LogInformation("БД успешно проинициализирована внутри инфраструктурного ядра.");

            // Собираем сервисы бизнес-логики один за другим
            _userRepository = new UserRepository(_connection, _userSqlGen);
            UserService = new UserService(_userRepository, _logger);
            ListUsersService = new ListUsersService(UserService);

            RegisterService = new RegisterService(UserService);
            AuthenticateService = new AuthenticateService(UserService);
            DeleteService = new DeleteService(UserService);
        }
    }



}
