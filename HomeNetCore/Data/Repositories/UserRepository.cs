using Dapper;
using HomeNetCore.Data.Generators.SqlQueriesGenerator;
using HomeNetCore.Helpers;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using Microsoft.Data.SqlClient;
using System.Data.Common;


namespace HomeNetCore.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;
        IUserSqlGenerator _userSqlGenerator;
        private readonly ILogger _logger;

        public UserRepository(DbConnection connection, ILogger logger, IUserSqlGenerator queryGenerator)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _userSqlGenerator = queryGenerator;
        }

        
            public async Task<bool> EmailExistsAsync(string email)
            {
                var sql = _userSqlGenerator.GenerateEmailExists();
                return await _connection.ExecuteScalarAsync<bool>(sql, new { email = email });
            }

            public async Task<UserEntity> InsertUserAsync(UserEntity user)
            {
                try
                {
                    var sql = _userSqlGenerator.GenerateInsert();
                    var newId = await _connection.ExecuteScalarAsync<int>(sql, user);
                    user.Id = newId;
                    return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Ошибка при вставке: {ex.Message}");
                    throw;
                }
            }


            public async Task DeleteByIdAsync(int id)
        {

            var affectedRows =
            await _connection.ExecuteAsync(_userSqlGenerator.GenerateDelete(), new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Пользователь с ID {id} не найден.");
            }

            _logger.LogInformation($"Пользователь с ID {id} удалён.");
        }


        public async Task<List<UserEntity>> GetAllAsync()
        {
            string sql = _userSqlGenerator.GenerateSelectAll();

            try
            {
                // Выполняем запрос через Dapper
                var users = (await _connection.QueryAsync<UserEntity>(sql)).ToList();

                // Проверяем результат
                if (users == null)
                {
                    throw new InvalidOperationException("Не удалось получить данные из БД");
                }

                _logger.LogInformation($"Получено {users.Count} пользователей.");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователей из БД", ex.Message);
                throw;
            }
        }


        public async Task<UserEntity?> GetByIdAsync(int id)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>
            (_userSqlGenerator.GenerateSelectById(), new { id = id });
        }


        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>
           (_userSqlGenerator.GenerateSelectByEmail(), new { Email = email });
        }


        public async Task UpdateAsync(UserEntity user)
        {
            await _connection.ExecuteAsync
           (_userSqlGenerator.GenerateUpdate(), user);

            _logger.LogInformation($"Пользователь {user.Id} обновлён.");
        }
    }
}
