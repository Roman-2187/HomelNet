using Dapper;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using Microsoft.Data.SqlClient;
using System.Data.Common;


namespace HomeNetCore.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;
        ISchemaUserSqlGenerator _userSqlGenerator;
        

        public UserRepository(DbConnection connection, ISchemaUserSqlGenerator queryGenerator)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
           
            _userSqlGenerator = queryGenerator;
        }


        public async Task<bool> EmailExistsAsync(string? email)
        {
            var sql = _userSqlGenerator.GenerateEmailExists();
            return await _connection.ExecuteScalarAsync<bool>(sql, new { email });
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
                throw new NotFoundException($"Ошибка при вставке: {ex.Message}");
                               
            }
        }


        public async Task DeleteByIdAsync(int id)
        {

            var affectedRows =
            await _connection.ExecuteAsync(_userSqlGenerator.GenerateDelete(), new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Пользователь  с ID {id} не найден.");
            }

            
        }


        public async Task<List<UserEntity>> GetAllAsync()
        {
            string sql = _userSqlGenerator.GenerateSelectAll();

            try
            {
                // Выполняем запрос через Dapper
                var users = (await _connection.QueryAsync<UserEntity>(sql)).ToList();

                // Проверяем результат
                return users ?? throw new InvalidOperationException("Не удалось получить данные из БД");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($" не удалось  выполнить запрос получения пользователей {ex.Message} ");

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

            
        }
    }
}
